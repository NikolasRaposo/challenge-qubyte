# Scene Entry Flow — Arquitetura e Rastreabilidade

Este documento apresenta a arquitetura do fluxo de entrada da cena — Menu Inicial → Loading → Cinemática → Handoff para Gameplay — e detalha os recursos de rastreabilidade e descoberta via editores “Varrer”, que tornam a integração e manutenção mais rápida e visual para equipes.

## Objetivos

- Separar responsabilidades em componentes pequenos e previsíveis.
- Evitar acoplamentos rígidos a implementações específicas (ex.: controlador do jogador).
- Minimizar dependências de singletons, mas utilizar os existentes quando agregarem valor (ex.: `InputContextCoordinator`).
- Fornecer uma fachada simples para quem integra a cena (um único ponto de orquestração).
- Manter pontos de extensão claros e documentação de contratos mínimos.
- Facilitar o gerenciamento e a descoberta com editores que “varrem” a cena e mostram quem dispara o fluxo e quem inicia as animações.

## Visão Geral

- Facade: `SceneEntryFlowCoordinator` orquestra as fases e conversa com controladores especializados.
- Fases especializadas: UI, Loading, Cinemática e Handoff para Player.
- Gate de controle do Player: utilitário para travar/destravar física e habilitar/desabilitar controlador.
- Contexto de Input: coordenado por `InputContextCoordinator` (existente no projeto).
- Sinais de Timeline/Animator: recebidos via `SignalReceiver` e/ou `NotifyAnimationEndSMB` + `AnimationEndHandler`.
- Descoberta visual: editores customizados permitem “Varrer” a cena para identificar os ativadores do fluxo e os iniciadores de animações ligados a um `Animator` específico.

> Nota: Um coordenador reutilizável já foi criado em `Assets/Scripts/UI/SceneEntryFlowCoordinator.cs`. A arquitetura abaixo formaliza sua função como fachada e delimita controladores auxiliares.

## Nomenclatura e Escopo

- O componente continua atuando como uma fachada: concentra orquestração, aplica políticas simples e delega execução às fases especializadas quando presentes (UI, Loading, Cinemática, PlayerGate). Ele mantém lógica mínima de fallback quando controladores estão ausentes.
- Com a adição do método público `ActivateAndPlayCinematic()`, o sistema ficou ainda mais reutilizável, pois outros componentes/eventos podem iniciar a cinemática em um único ponto de extensão, sem duplicar ativação e `Play()`.
- O nome atual `SceneEntryFlowCoordinator` comunica bem o escopo principal (fluxo de entrada da cena: menu → loading → cinemática → gameplay). Se o objetivo evoluir para coordenar também outros fluxos intra‑jogo (ex.: transições de fases após boss, cutscenes no meio do jogo), considere:
  - `EntryFlowCoordinator` para enfatizar o foco no fluxo de entrada, mas com menor acoplamento ao termo “Scene”.
  - `FlowCoordinator` se a intenção for torná-lo um coordenador genérico de múltiplos fluxos (entrada e mid‑game), mantendo perfis/flags para comportamentos.
- Recomendações práticas:
  - Mantenha `SceneEntryFlowCoordinator` enquanto o uso principal for o fluxo inicial. 
  - Só migre para `FlowCoordinator` se o escopo do componente realmente abarcar múltiplos fluxos além do entry, para evitar nomes genéricos demais sem necessidade.

## Componentes e Responsabilidades

### 1) SceneEntryFlowCoordinator (Fachada)

- Responsabilidade: orquestrar o fluxo completo; coordenar troca de contextos de input; chamar controladores de UI/Loading/Cinemática; entregar o controle ao Player no final.
- Entradas: referências opcionais a `PlayableDirector`, `Animator` de Loading, Canvas de UI/HUD, botão padrão.
- Saídas: logs, eventos simples (ex.: `OnLoadingFinished`, `OnCinematicFinished`).
- Políticas configuráveis via flags: entrar em UI, bloquear gameplay, habilitar interações de UI, auto-play da cinemática, travar/destravar física e habilitar/desabilitar controlador.

Principais métodos públicos (C#):

```csharp
// Já presente no projeto
void ActivateLoadingUI();
void DeactivateLoadingUI();
void OnLoadingFinished();
void OnCinematicFinished();
void EnterUiContextWithFocus(GameObject preferred);
void EnterPlayerContext();
void EnterBlockInputContext();
void ActivateAndPlayCinematic();
```

Editor associado
- Foldout “Ativadores do fluxo”: botão “Varrer UnityEvents na cena” que identifica eventos persistentes chamando métodos públicos da fachada (como os acima) e lista objetos ativadores com links clicáveis.
- Botão “Revarrer” para atualizar resultados após mudanças de configuração.

### 2) UiPhaseController

- Responsabilidade: lidar com visibilidade de Canvas/UI, foco inicial no `EventSystem`, habilitar/desabilitar interações de UI.
- Interação: chamado pela fachada em `Start` e na transição para Player.

API mínima sugerida:

```csharp
public class UiPhaseController : MonoBehaviour {
  public GameObject canvasUI;
  public GameObject defaultButton;
  public void EnterUi(InputContextCoordinator icc);
  public void ExitUi(InputContextCoordinator icc);
}
```

### 3) LoadingPhaseController

- Responsabilidade: iniciar/parar animação de Loading (via `Animator`), emitir evento quando a carga terminar.
- Interação: a fachada chama `StartLoading()`, recebe `OnLoadingFinished()` do sistema de loading/animator.

API mínima sugerida:

```csharp
public class LoadingPhaseController : MonoBehaviour {
  public Animator loadingAnimator;
  public string startTrigger = "StartLoading";
  public string stopTrigger  = "StopLoading";
  public event Action OnLoadingFinished; // emitido ao finalizar
  public void StartLoading(InputContextCoordinator icc);
  public void StopLoading();
}
```

### 4) CinematicPhaseController

- Responsabilidade: tocar `PlayableDirector`, ouvir fim da timeline via `SignalReceiver` ou `NotifyAnimationEndSMB`/`AnimationEndHandler`, emitir evento de término.
- Interação: a fachada chama `Play()`, recebe `OnCinematicFinished()` e faz handoff para Player.

API mínima sugerida:

```csharp
public class CinematicPhaseController : MonoBehaviour {
  public PlayableDirector director;
  public event Action OnCinematicFinished;
  public void Play();
  public void NotifyEnd(); // chamado por SignalReceiver/SMB
}
```

### 5) PlayerControlGate

- Responsabilidade: travar/destravar física (`Rigidbody.isKinematic`), habilitar/desabilitar controlador (`Behaviour`, ex.: `ECMSaciController`).
- Interação: a fachada chama `Freeze/Unfreeze` e `Enable/Disable` conforme flags.

API mínima sugerida:

```csharp
public class PlayerControlGate : MonoBehaviour {
  public Rigidbody playerRb;          // opcional
  public Behaviour playerController;  // opcional
  public void FreezePhysics();
  public void UnfreezePhysics();
  public void DisableController();
  public void EnableController();
}
```

### 6) InputContextCoordinator (existente)

- Responsabilidade: trocar entre contextos `UI`, `Player`, `BlockInput`; habilitar/desabilitar `InputSystemUIInputModule`.
- Interação: usado por fachada e pelo `UiPhaseController` em pontos-chave do fluxo.

### 7) Timeline/Animator Signals

- Responsabilidade: padronizar o envio de eventos de fim de animação/cinemática.
- Interação: `SignalReceiver` chama `CinematicPhaseController.NotifyEnd()`; `NotifyAnimationEndSMB` chama `AnimationEndHandler` e este encaminha para quem precisa.

## Fluxo Operacional (Sequência)

```mermaid
sequenceDiagram
  participant Facade as SceneEntryFlowCoordinator
  participant UI as UiPhaseController
  participant Load as LoadingPhaseController
  participant Cine as CinematicPhaseController
  participant Gate as PlayerControlGate
  participant ICC as InputContextCoordinator

  Facade->>ICC: SetUiContext()
  Facade->>UI: EnterUi(ICC)
  Facade->>Load: StartLoading(ICC)
  Load-->>Facade: OnLoadingFinished
  Facade->>Facade: ActivateAndPlayCinematic() // ativa raiz e dá Play
  Facade->>Cine: Play() // via controlador, quando presente
  Cine-->>Facade: OnCinematicFinished
  Facade->>ICC: SetPlayerContext(); DisableUiInteractions()
  Facade->>Gate: UnfreezePhysics(); EnableController()
```

Rastreabilidade do fluxo via editores
- “Ativadores do fluxo” no `SceneEntryFlowCoordinator`: mostra quem chama métodos como `EnterUiContextWithFocus` e `OnCinematicFinished` por meio de `UnityEvents`, facilitando localizar o ponto de disparo na cena.
- “Rastreamento: origem da animação” no `AnimationEndHandler`: identifica `UnityEvents` que chamam métodos em componentes que referenciam o mesmo `Animator` do handler, revelando quem inicia a animação que ele escuta.
- Encadeamento típico: “animação de empresa termina” → `UnityEvent` inicia animação do menu principal (no mesmo `Animator`) → `AnimationEndHandler` detecta fim da animação → chama `SceneEntryFlowCoordinator.EnterUiContextWithFocus()`.

## Contratos Mínimos e Dependências

- A fachada não conhece implementações internas; ela só invoca métodos públicos simples.
- Nenhum `FindObjectOfType` automático além de capturas seguras (ex.: pegar `PlayableDirector` do `cinematicRoot` se presente).
- Dependência com `GameManager` é opcional e limitada a marcar `SetStartMenuActive(true/false)` quando necessário.
- Todos os controladores devem funcionar mesmo com referências nulas (fazem log e seguem), para evitar bloqueios.
- Os editores suportam seleção múltipla quando aplicável e focam em `UnityEventBase` persistente, evitando varreduras invasivas e exceções de referências não atribuídas.

## Estrutura de Pastas Sugerida

```
Assets/
  Scripts/
    UI/
      SceneEntryFlowCoordinator.cs        # fachada
      UiPhaseController.cs                # controle de UI
      LoadingPhaseController.cs           # controle de Loading
      CinematicPhaseController.cs         # controle de Cinemática
      PlayerControlGate.cs                # gate de física/controlador
    Managers/
      InputContextCoordinator.cs          # existente
      GameManager.cs                      # existente
```

## Perfis e Parametrização (Opcional)

- `SceneEntryFlowProfile` (`ScriptableObject`): define políticas por cena (ex.: travar física no início, auto-play cinematica, triggers de Loading).
- Vantagem: reduz duplicação de configuração entre cenas; cuidado para não complicar o MVP.

## Decisões para Evitar Over-Engineering

- Sem heranças complexas; uso de classes concretas pequenas com APIs claras.
- Interfaces são opcionais; só se a equipe precisar trocar implementações em runtime.
- Eventos simples (`Action`) para sinalizar término de fases; sem bus global.
- Configuração via Inspector; apenas o essencial em cada controlador.
- Edidores “Varrer” fornecem observabilidade sem acoplamento adicional no runtime.

## Migração de StartMenuControlAnim

- Passo 1: Inserir `SceneEntryFlowCoordinator` na cena e apontar referências existentes (UI/HUD, Loading Animator, Director, botão padrão).
- Passo 2: Desligar gradualmente responsabilidades do `StartMenuControlAnim` e realocar em `UiPhaseController`, `LoadingPhaseController`, `CinematicPhaseController`, `PlayerControlGate`.
- Passo 3: Manter um wrapper temporário com logs para comparar comportamentos e evitar regressões.
- Passo 4: Remover campos/acoplamentos rígidos apenas quando os novos controladores estiverem validados pelo QA.

## QA Resumido (Pós-Merge)

- UI: foco no botão inicial, interações ativas; `SetUiContext()` no início.
- Loading: triggers disparam; `OnLoadingFinished` chega na fachada.
- Cinemática: `ActivateAndPlayCinematic()` ativa a raiz e inicia a Timeline; sinal de fim aciona `OnCinematicFinished`.
- Handoff: `SetPlayerContext()` e `DisableUiInteractions()`; física destravada e controlador habilitado.
- Logs: presentes em cada fase; sem exceções quando referências são nulas.
- Editores: “Ativadores do fluxo” lista ativadores com links; “Rastreamento: origem da animação” exibe iniciadores ligados ao `Animator` — use “Revarrer” após mudanças.

## FAQ de Arquitetura

- Posso usar sem Timeline? Sim, `CinematicPhaseController` pode ser omitido; chame `OnCinematicFinished()` manualmente.
- E se não houver Loading? Use `LoadingPhaseController` como stub ou remova; a fachada segue.
- Preciso de perfis? Só se várias cenas tiverem políticas distintas repetitivas; caso contrário mantenha flags.
- Onde plugar novos sinais? No `SignalReceiver` da Timeline, chamando `CinematicPhaseController.NotifyEnd()`.
- Como descubro quem inicia a animação do menu? Abra o Inspector do `AnimationEndHandler` no menu, use “Varrer origem” e clique nos links para navegar aos objetos iniciadores.
- Quem chama `EnterUiContextWithFocus`? No `SceneEntryFlowCoordinator`, use “Varrer UnityEvents na cena” para listar os ativadores que chamam métodos da fachada.

## Próximos Passos

- Opcional: criar `UiPhaseController.cs`, `LoadingPhaseController.cs`, `CinematicPhaseController.cs`, `PlayerControlGate.cs` com as APIs mínimas acima.
- Migrar uma cena piloto substituindo `StartMenuControlAnim` por `SceneEntryFlowCoordinator` + controladores.
- Atualizar o playbook QA no `Cinematic_StartMenu_Input_Integration.md` referenciando os novos controladores.
- Padronizar uso dos editores: durante integração e revisão, sempre utilizar “Varrer” para confirmar disparadores e encadeamentos.

---

## Guia de Uso dos Editores “Varrer”

### SceneEntryFlowCoordinator — Ativadores do fluxo
- Abra o `SceneEntryFlowCoordinator` no Inspector.
- Expanda “Ativadores do fluxo”.
- Clique em “Varrer UnityEvents na cena” para identificar quem chama métodos públicos da fachada.
- Use os links clicáveis para selecionar rapidamente o objeto ativador e revisar sua configuração.
- Clique em “Revarrer” após qualquer mudança nos `UnityEvents` ou na cena.

### AnimationEndHandler — Rastreamento: origem da animação
- Abra o `AnimationEndHandler` vinculado ao `Animator` do menu (ou outra animação relevante).
- Expanda “Rastreamento: origem da animação”.
- Clique em “Varrer origem” para descobrir `UnityEvents` que chamam métodos em componentes que referenciam o mesmo `Animator` do handler.
- Na multi-seleção, use “Varrer origem (N selecionados)” para ver blocos por objeto, cada um com seus iniciadores e links.

### Boas práticas e limitações
- Configure eventos via Inspector (persistentes) para que sejam detectáveis pelo “Varrer”. Eventos criados apenas em runtime não aparecem.
- O rastreador de animação usa uma heurística baseada em referência ao mesmo `Animator`. Se o handler ouvir outro `Animator`, considere expor um campo “Animator escutado”.
- Evitamos enumerar coleções arbitrárias e tratamos exceções ao ler campos, prevenindo erros como `UnassignedReferenceException` (ex.: ao varrer `Transform` inadvertidamente).
- Use nomes de métodos por convenção (ex.: `Play`, `SetTrigger`, `Start`) para facilitar a leitura, embora não seja obrigatório.

### Cenário de referência (introdução da empresa → menu principal)
- A animação da empresa dispara um `UnityEvent` de fim (`onEnd`) que inicia a animação do menu principal (mesmo `Animator`).
- O `AnimationEndHandler` observa o fim da animação do menu e chama `SceneEntryFlowCoordinator.EnterUiContextWithFocus()`.
- Com os editores, você vê: quem iniciou a animação do menu e quem chamou os métodos do fluxo — navegando com links e confirmando o encadeamento sem esforço.

## Capítulo Visual: Painéis “Varrer”

Use este guia rápido com imagens/GIFs para acelerar o onboarding da equipe e padronizar a inspeção do fluxo.

### FlowCoordinator — Ativadores do fluxo
- Antes de varrer:
  - Abra o `SceneEntryFlowCoordinator` e expanda “Ativadores do fluxo”.
  - Imagem: `./img/flow_varrer_before.png`
- Depois de varrer:
  - Clique em “Varrer UnityEvents na cena” e veja a lista de ativadores.
  - Imagem: `./img/flow_varrer_after.png`
- Navegação por link:
  - Clique no nome sublinhado para selecionar e dar ping no iniciador.
  - GIF: `./img/flow_link_ping.gif`

### AnimationEndHandler — Origem da animação
- Varredura simples:
  - Expanda “Rastreamento: origem da animação” e clique em “Varrer origem”.
  - Imagem: `./img/anim_varrer_single.png`
- Multi-seleção:
  - Selecione vários `AnimationEndHandler` e clique em “Varrer origem (N selecionados)”.
  - Imagem: `./img/anim_varrer_multi.png`
- Navegação por link:
  - Clique no iniciador para selecionar e revisar o `UnityEvent`.
  - GIF: `./img/anim_link_ping.gif`

### Organização de arquivos de mídia
- Coloque imagens e GIFs em `Docs/img/` e use nomes descritivos.
- Recomenda-se resolução de 1280×720 para imagens; GIFs otimizados até ~4–8 MB.
- Ferramentas sugeridas:
  - Captura: ShareX, OBS ou a ferramenta de captura do SO.
  - Otimização: ImageOptim/PNGGauntlet (imagens), gifski/ffmpeg (GIFs).

### Dicas de captura
- Mostre o estado “antes” e “depois” de clicar em “Varrer”.
- Inclua um exemplo de link clicado com seleção/ping de objeto.
- Em multi-seleção, capture pelo menos dois objetos com resultados contrastantes (um com iniciadores, outro vazio).

### Observabilidade padronizada
- Durante reviews de integração, utilize sempre os painéis “Varrer” para confirmar disparadores e encadeamentos.
- Anexe as imagens/GIFs relevantes em PRs para facilitar o QA e a comunicação entre times.