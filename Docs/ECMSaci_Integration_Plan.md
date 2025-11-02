# Plano de Integração: ECMSaciController + Gameplay/Player Helpers

Este documento mapeia os componentes de gameplay/efeitos (MagneticField, CoinAttractor, BoxInteractor, TrampolimController, CoinPickup) frente ao novo `ECMSaciController` e ao ECM (Easy Character Movement), propondo ajustes para garantir fluidez e consistência técnica.

## Objetivos

- Garantir compatibilidade com o `ECMSaciController` (ECM) sem dependências de `ThirdPersonController` antigo.
- Evitar duplicidade de funcionalidades e adaptar chamadas de física/movimento à API do ECM.
- Melhorar responsividade e estabilidade (sem flickers/glitches) em interações como trampolim, caixas interativas e magnetismo de moedas.

## Contexto: ECM + ECMSaciController

- `ECMSaciController` herda de `BaseCharacterController` e trabalha com `CharacterMovement` (`movement`):
  - APIs úteis: `movement.velocity`, `movement.isGrounded`, `movement.groundPoint`, `movement.groundNormal`, `movement.ApplyVerticalImpulse(float)`, `movement.DisableGrounding()`.
  - Eventos e estados: Animate() atualiza parâmetros (Speed, VerticalVelocity, FreeFall, CanDoubleJump) e gerencia `MidAirJumpCount`.
- Implicações:
  - Evitar aplicar forças diretas em `Rigidbody` do player (caso presente). Preferir `ApplyVerticalImpulse`/ajustes de velocidade via `CharacterMovement`.
  - Qualquer interação que dependa de “está caindo/aterrissou” deve usar `movement.isGrounded`, `velocity.y` e, quando aplicável, `groundPoint/groundNormal`.

## Diagnóstico dos Scripts Existentes

1) `PlayerMagneticField.cs`
- Depende de `ThirdPersonController` via `[RequireComponent]` (incompatível com ECMSaci).
- Cria um `SphereCollider` trigger filho para atrair moedas e usa `tag`/`layer` fixos.
- Pode funcionar, mas precisa remover dependência do controller antigo e generalizar tags/layers.

2) `PlayerCoinAttractor.cs`
- Supõe a existência de `PlayerMagneticField` e configura `MagneticTriggerHandler` no filho.
- Binds para um `attractionPoint` (ok), mas acopla nomes de objetos e fluxo atual.

3) `BoxInteractor.cs`
- Interage com `ThirdPersonController` (antigo) e, como fallback, com `Rigidbody`.
- Usa propriedades como `rb.linearVelocity` (não padrão em Unity, risco de inconsistência).
- Trampolim interno (`ApplyTrampolineEffect`) deveria integrar ao ECM via `ApplyVerticalImpulse`.

4) `TrampolimController.cs`
- Similar ao BoxInteractor: tenta achar `ThirdPersonController` ou aplicar força em `Rigidbody`.
- Animações e cooldowns são independentes (ok), mas a aplicação de impulso vertical/horizontal deve ser consistente com ECM.

5) `CoinPickup.cs`
- Lógica de magnetismo é própria: ativa por trigger `MagneticTrigger`, MoveTowards para o attraction point, coleta por distância.
- Não conflita com ECM diretamente; principais pontos são integração com triggers e evitar colisões acidentais com o player.

## Plano de Integração por Componente

1) PlayerMagneticField
- Remover `[RequireComponent(typeof(ThirdPersonController))]` e tornar genérico.
- Expor `LayerMask` e `tag` configuráveis para o trigger magnético, evitando hardcodes.
- Garantir que o trigger filho não interfira em grounding/colisões do ECM (`isTrigger = true`, camada dedicada).
- Opcional: publicar evento `OnCoinEnteredMagneticField(CoinPickup coin)` para integração futura.

2) PlayerCoinAttractor
- Manter criação/atribuição de `attractionPoint` (configurável via Inspector).
- Generalizar busca do filho “MagneticField” (usar referência direta ao componente em vez de nome fixo), ou expor um campo para o transform do trigger.
- Consolidar o `MagneticTriggerHandler` para não depender de tags mágicas: usar referência direta ao `PlayerMagneticField` e encaminhar `attractionPoint`.

3) BoxInteractor
- Substituir tentativa de obter `ThirdPersonController` por `ECMSaciController` (ou interface comum `ICharacterJumpImpulse`).
- Para trampolim interno: usar `movement.ApplyVerticalImpulse(force)` do `ECMSaciController.movement`.
- Horizontal: ajustar `movement.velocity` projetando no plano horizontal e aplicando multiplicador (sem setar `Rigidbody` direto).
- Remover usos de `rb.linearVelocity` (trocar por `rb.velocity` se necessário em objetos genéricos não-player).
- Compatibilidade: se o interactor atingir objetos não-player, manter fallback para `Rigidbody` com `rb.AddForce(..., ForceMode.VelocityChange)`.

4) TrampolimController
- Detectar `ECMSaciController` e aplicar impulso via ECM:
  - Vertical: `movement.ApplyVerticalImpulse(launchForce)` (ajustar para unidade consistente com alturas/jump do ECM).
  - Horizontal: preservar e multiplicar velocidade horizontal do `movement.velocity` (e.g., `Vector3 lateral = Vector3.ProjectOnPlane(velocity, transform.up) * horizontalMultiplier`).
- Se não for player ECM: fallback para `Rigidbody` com API padrão (`velocity`, `AddForce`).

5) CoinPickup
- Confirmar que o trigger de magnetismo usa `isTrigger` e camadas que não colidem com o capsule/ECM (evitar empurrões).
- Manter `MoveTowards` (suave) e distância mínima de coleta.
- Opcional: interpolar com `Vector3.Lerp` baseado em distância para suavizar aceleração.
- Garantir que a coin não bloqueie `groundPoint` do ECM (layer de coin sem afeitar grounding).

## Pontos de Complexidade

- Sincronização de física: ECM usa `CharacterMovement` com controle próprio de grounding e velocidade. Evitar escrever diretamente em `Rigidbody` do player.
- Conversão de forças: `ApplyVerticalImpulse` opera em termos de impulso físico (consistente com alturas definidas por `groundJumpHeight/midAirJumpHeight`). Ajustar trampolim/caixa para usar valores compatíveis.
- Interoperabilidade: Ao lidar com objetos não-player (caixas, trampolins genéricos), manter fallback com `Rigidbody` sem quebrar integração ECM.
- Layers/Tags: padronizar para evitar colisões indesejadas com capsule do ECM e triggers de magnetismo.

## Melhorias com uso de Rigidbody/Movement

- Player (ECM):
  - Preferir `movement.ApplyVerticalImpulse`, `movement.DisableGrounding` e ajustes em `movement.velocity` para manipular deslocamentos.
  - Usar `movement.groundPoint`/`groundNormal` para efeitos visuais de contato (já usado pelo VFX controller).
- Trampolim/Caixa:
  - Ao invés de setar `Rigidbody.velocity` do player, calcular e aplicar impulso via ECM, mantendo previsibilidade com alturas.
  - Preservar direção lateral e permitir multiplicador horizontal apenas no plano do chão.

## API/Assinaturas Propostas (exemplos)

- BoxInteractor (player ECM)

```csharp
if (interactor.TryGetComponent(out ECMSaciController saci)) {
    var mv = saci.movement;
    // vertical
    mv.ApplyVerticalImpulse(trampolineForce);
    // horizontal opcional
    var vel = mv.velocity;
    var lateral = Vector3.ProjectOnPlane(vel, saci.transform.up) * horizontalVelocityMultiplier;
    mv.velocity = new Vector3(lateral.x, mv.velocity.y, lateral.z);
}
else if (interactor.TryGetComponent(out Rigidbody rb)) {
    rb.velocity = new Vector3(rb.velocity.x * horizontalVelocityMultiplier, 0f, rb.velocity.z * horizontalVelocityMultiplier);
    rb.AddForce(Vector3.up * trampolineForce, ForceMode.VelocityChange);
}
```

- TrampolimController (player ECM)

```csharp
if (targetObject.TryGetComponent(out ECMSaciController saci)) {
    var mv = saci.movement;
    mv.ApplyVerticalImpulse(launchForce);
    var vel = mv.velocity;
    var lateral = Vector3.ProjectOnPlane(vel, saci.transform.up) * horizontalVelocityMultiplier;
    mv.velocity = lateral + Vector3.up * mv.velocity.y;
}
```

- PlayerMagneticField (genérico)

```csharp
[DisallowMultipleComponent]
public class PlayerMagneticField : MonoBehaviour {
    [SerializeField] private LayerMask magneticLayer;
    [SerializeField] private string magneticTag = "MagneticTrigger";
    // ... criar trigger filho com isTrigger=true, layer/tag configuráveis
}
```

## Migração e Compatibilidade

- Remover dependências explícitas de `ThirdPersonController` nos scripts de gameplay (usar ECMSaciController ou caminho genérico).
- Manter fallback para `Rigidbody` apenas para objetos não-player.
- Padronizar layers: `PlayerMagneticField`, `Coins`, `Interactive` para evitar interferência com grounding do ECM.

## Testes e Validação

- Trampolim: validar alturas e velocidades com e sem multiplicadores, incluindo quedas e double jump próximo ao chão (sem VFX de aterrissagem indevido).
- Caixas: confirmar comportamento em interações múltiplas (explodir, desaparecer, respawn) e trampolim embutido.
- Magnetismo: garantir atração gradual, sem empurrar o player, e coleta suave.
- Animator: `CanDoubleJump` responde ao cooldown; `FreeFall` reflete quedas reais.

## Próximos Passos

1) Refatorar `PlayerMagneticField` e `PlayerCoinAttractor` para remover acoplamentos a nomes/tags fixos e ao `ThirdPersonController`.
2) Atualizar `BoxInteractor` e `TrampolimController` para usar `ECMSaciController`/`CharacterMovement` como caminho principal.
3) Revisar `CoinPickup` para garantir camadas corretas e suavidade do movimento magnético.
4) Criar um utilitário comum (`MovementUtils`) para operações horizontais (projeção, multiplicadores) e reduzir duplicação.
5) Validar em cenas de teste (quedas longas, trampolim, spam de pulo) e ajustar constantes.

—

Documento de trabalho; sujeito a ajustes conforme testes e feedback de jogabilidade.