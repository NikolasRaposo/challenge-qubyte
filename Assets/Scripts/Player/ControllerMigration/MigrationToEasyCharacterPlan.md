# Plano de Migração: ThirdPersonController para Easy Character Movement (ECM)

## 1. Análise da Estrutura Atual vs ECM

### Sistema Atual (ThirdPersonController)
- **Base**: Unity CharacterController
- **Física**: Kinematic (não usa Rigidbody physics)
- **Movimento**: Baseado em `CharacterController.Move()`
- **Gravidade**: Aplicada manualmente via `_verticalVelocity`
- **Detecção de Chão**: Sphere overlap check manual
- **Plataformas**: Sistema customizado com `IPlatformVelocityProvider`
- **Input**: Integrado com `StarterAssetsInputs` e `InputModeManager`
- **Câmera**: Sistema de dolly tracks (não third-person tradicional)

### Sistema ECM (Easy Character Movement)
- **Base**: Unity Rigidbody + CapsuleCollider
- **Física**: Rigidbody physics completo
- **Movimento**: Baseado em forças e impulsos do Rigidbody
- **Gravidade**: Vector3 customizável (permite gravidade arbitrária)
- **Detecção de Chão**: Sistema robusto `GroundDetection` com múltiplas verificações
- **Plataformas**: Suporte nativo para plataformas móveis e rotativas
- **Input**: Sistema flexível via `BaseCharacterController.HandleInput()`
- **Câmera**: Suporte para múltiplos tipos de câmera

## 2. Benefícios Identificados do ECM para Jogos de Plataforma

### 2.1 Física Realista
- **Interação com objetos**: Rigidbody permite empurrar/ser empurrado por outros objetos
- **Impulsos**: Sistema de impulsos para bounces, knockbacks, etc.
- **Plataformas móveis**: Suporte nativo e estável para plataformas animadas
- **Detecção de colisão contínua**: Evita atravessar objetos em alta velocidade

### 2.2 Sistema de Movimento Avançado
- **Ground Snap**: Mantém personagem colado ao chão em rampas e superfícies irregulares
- **Slope Handling**: Controle preciso de slopes com sliding automático
- **Step Detection**: Capacidade de subir degraus automaticamente
- **Ledge Handling**: Controle de bordas com offset configurável

### 2.3 Flexibilidade
- **Gravidade Arbitrária**: Permite mecânicas como wall-walking, gravidade invertida
- **Rotação Livre**: Personagem pode ser rotacionado em qualquer eixo
- **Root Motion**: Suporte nativo para animações com root motion
- **Crouch System**: Sistema de agachamento integrado

## 3. Estrutura Core do ECM

### 3.1 Componentes Principais
- **CharacterMovement**: Motor principal (equivale ao CharacterController)
- **BaseCharacterController**: Classe base para controladores
- **GroundDetection**: Sistema avançado de detecção de chão
- **RootMotionController**: Para animações com root motion

### 3.2 Hierarquia de Classes
```
BaseCharacterController (abstract)
├── BaseFirstPersonController
├── BaseThirdPersonController
└── Custom Controllers (MyCharacterController)
```

## 4. Plano de Integração com Sistemas Existentes

### 4.1 InputModeManager
**Status**: ✅ Compatível - Requer adaptação mínima

**Estratégia**:
- Manter `InputModeManager` como está
- Adaptar `HandleInput()` no novo controlador ECM
- Usar `StarterAssetsInputs` como fonte de input
- Mapear inputs existentes para o sistema ECM

**Implementação**:
```csharp
protected override void HandleInput()
{
    // Usar InputModeManager existente
    var inputs = GetComponent<StarterAssetsInputs>();
    
    moveDirection = new Vector3(inputs.move.x, 0f, inputs.move.y);
    jump = inputs.jump;
    // ... outros inputs
}
```

### 4.2 Sistema de Animação (Saci)
**Status**: ⚠️ Requer adaptação significativa

**Desafios**:
- ECM usa diferentes parâmetros de animação
- Sistema atual pode estar acoplado ao CharacterController
- Necessário mapear estados de movimento para ECM

**Estratégia**:
1. **Análise do Animator**: Identificar parâmetros atuais
2. **Mapeamento de Estados**: Criar correspondência entre sistemas
3. **Implementação Gradual**: Migrar parâmetro por parâmetro
4. **Override do Animate()**: Customizar método de animação

### 4.3 Sistema de Câmera (Dolly Tracks)
**Status**: ✅ Compatível - Sem alterações necessárias

**Justificativa**:
- Sistema de dolly tracks é independente do controlador
- ECM não interfere com sistema de câmera existente
- Cinemachine continua funcionando normalmente

## 5. Estratégia de Migração

### 5.1 Fase 1: Preparação (1-2 dias)
- [ ] Backup completo do projeto
- [ ] Criar branch específico para migração
- [ ] Análise detalhada do animator do Saci
- [ ] Documentar todos os parâmetros de animação atuais
- [ ] Identificar dependências do ThirdPersonController

### 5.2 Fase 2: Implementação Base (2-3 dias)
- [ ] Criar `SaciECMController` estendendo `BaseCharacterController`
- [ ] Implementar `HandleInput()` com integração ao `InputModeManager`
- [ ] Configurar parâmetros básicos de movimento
- [ ] Testar movimento básico (andar, correr, pular)

### 5.3 Fase 3: Integração Avançada (3-4 dias)
- [ ] Implementar sistema de animação customizado
- [ ] Migrar mecânicas específicas (double jump, bounce on enemy)
- [ ] Integrar sistema de plataformas móveis
- [ ] Testar interação com objetos físicos

### 5.4 Fase 4: Refinamento (2-3 dias)
- [ ] Ajustar parâmetros de física para feel similar
- [ ] Otimizar performance
- [ ] Testes extensivos em todas as fases do jogo
- [ ] Correção de bugs e ajustes finais

## 6. Implementação Técnica Detalhada

### 6.1 Estrutura do Novo Controlador

```csharp
public class SaciECMController : BaseCharacterController
{
    [Header("Saci Specific")]
    [SerializeField] private float _bounceForceOnEnemy = 7f;
    [SerializeField] private float _doubleJumpHeight = 1.0f;
    
    // Referências aos sistemas existentes
    private StarterAssetsInputs _inputs;
    private InputModeManager _inputManager;
    private Animator _animator;
    
    protected override void HandleInput()
    {
        // Integração com InputModeManager
        moveDirection = new Vector3(_inputs.move.x, 0f, _inputs.move.y);
        jump = _inputs.jump;
        // ... outros inputs
    }
    
    protected override void Animate()
    {
        // Integração com animator do Saci
        // Mapear parâmetros ECM para parâmetros existentes
    }
}
```

### 6.2 Configuração de Componentes

**Componentes Necessários**:
- `Rigidbody` (substituir CharacterController)
- `CapsuleCollider` (configurar dimensões similares)
- `CharacterMovement` (motor principal)
- `GroundDetection` (detecção de chão)
- `SaciECMController` (controlador customizado)

**Configurações Recomendadas**:
```
Rigidbody:
- Mass: 1
- Drag: 0
- Angular Drag: 0
- Use Gravity: false (ECM controla)
- Is Kinematic: false
- Freeze Rotation: X, Z (manter Y livre)

CharacterMovement:
- Max Lateral Speed: 5.0 (similar ao atual)
- Gravity: (0, -15, 0) (similar ao atual)
- Slope Limit: 45°
- Snap To Ground: true
```

### 6.3 Hierarquia do GameObject (SaciECMController)

```
SaciECMController (GameObject Pai)
├── Components (no objeto pai)
│   ├── Rigidbody
│   ├── CapsuleCollider
│   ├── CharacterMovement
│   ├── GroundDetection
│   ├── SaciECMController (Script)
│   ├── PlayerInput
│   ├── StarterAssetsInputs
│   ├── InputModeManager
│   └── (Opcional) Componente de áudio / efeitos específicos
├── SaciModel (Filho)
│   ├── SkinnedMeshRenderer / Meshes
│   ├── Animator (Controller: Saci_ECM_Animator.controller)
│   └── (Opcional) RootMotionController (helper do ECM)
└── CinemachineCameraTarget (Filho)
    └── Empty / Pivot para câmera (se necessário)
```

- O objeto pai `SaciECMController` agrupa toda a lógica de movimento e input.
- O filho `SaciModel` contém o modelo 3D do Saci e o `Animator` (com o novo controller dedicado).
- `RootMotionController` pode ser adicionado ao mesmo GameObject do `Animator` caso usemos root motion (rotação e/ou deslocamento) conforme suporte do ECM.
- `CinemachineCameraTarget` é um filho vazio usado como alvo de câmera para seguir/rotacionar com maior controle (compatível com Dolly Tracks).

### 6.4 Guia: Criar Novo Animator Controller (Saci_ECM_Animator)

- **Objetivo**: Manter o Animator atual intacto e criar um novo controller dedicado ao ECM para maior liberdade.

**Passo a passo**:
1. Criar um `Animator Controller` chamado `Saci_ECM_Animator.controller` em `Assets/_Project/Animations/Controllers` (ou pasta padrão do projeto).
2. No `SaciModel` (filho), atribuir este controller ao componente `Animator`.
3. Adicionar os seguintes parâmetros ao Animator:
   - `Speed` (float): magnitude da velocidade lateral (usado para blend Idle/Walk/Run).
   - `IsGrounded` (bool): estado de contato com o chão.
   - `VerticalVelocity` (float): velocidade vertical (para estados de queda / subida).
   - `IsJumping` (bool): estado de pulo ativo.
   - `IsCrouching` (bool): agachado ou não.
   - (Opcional) `JumpTrigger` (trigger): para iniciar animação de pulo.
4. Criar um `Blend Tree` de Locomoção (Base Layer):
   - Entrada: `Speed`
   - Clips: `Idle`, `Walk`, `Run` (transições suaves por velocidade)
5. Criar camada / sub-estados de Aéreo:
   - Estados: `Jump`, `Fall`, `Land`
   - Transições: `JumpTrigger`/`IsJumping` para `Jump`; `VerticalVelocity < 0` e `!IsGrounded` para `Fall`; `IsGrounded` para `Land`.
6. Adicionar estado de `Crouch` em sub-máquina ou camada separada:
   - Ativar quando `IsCrouching == true`.
7. Configurar **Root Motion** conforme necessidade:
   - Se usar root motion de rotação, habilitar `_useRootMotion = true` e `_rootMotionRotation = true` no controlador.
   - Adicionar `RootMotionController` ao GameObject do `Animator` se usar root motion (compatível com ECM 1.8+).
8. No `SaciECMController.Animate()` mapear os parâmetros:

```csharp
protected override void Animate()
{
    if (_animator == null)
        _animator = GetComponentInChildren<Animator>();

    var lateralSpeed = Vector3.ProjectOnPlane(movement.velocity, transform.up).magnitude;

    _animator.SetFloat("Speed", lateralSpeed);
    _animator.SetBool("IsGrounded", movement.isGrounded);
    _animator.SetFloat("VerticalVelocity", movement.velocity.y);
    _animator.SetBool("IsJumping", _isJumping);
    _animator.SetBool("IsCrouching", isCrouching);
}
```

- Ajustar nomes de clips e thresholds no `Blend Tree` conforme os assets de animação do Saci.
- Manter o Animator antigo intacto e referenciável para fallback em caso de testes A/B.

## 7. Riscos e Mitigações

### 7.1 Riscos Identificados
1. **Performance**: Rigidbody pode ser mais pesado que CharacterController
2. **Feel do Movimento**: Física pode alterar sensação do controle
3. **Compatibilidade**: Sistemas existentes podem quebrar
4. **Animações**: Parâmetros podem não mapear corretamente

### 7.2 Mitigações
1. **Performance**: Otimizar configurações, usar Fixed Timestep adequado
2. **Feel**: Ajustar parâmetros gradualmente, manter referências do sistema atual
3. **Compatibilidade**: Testes extensivos, implementação gradual
4. **Animações**: Análise prévia, mapeamento cuidadoso de parâmetros

## 8. Cronograma Estimado

**Total**: 8-12 dias de desenvolvimento

- **Semana 1**: Fases 1 e 2 (Preparação + Implementação Base)
- **Semana 2**: Fases 3 e 4 (Integração Avançada + Refinamento)

## 9. Critérios de Sucesso

### 9.1 Funcionalidades Básicas
- [ ] Movimento (andar, correr) funciona identicamente
- [ ] Sistema de pulo (simples e duplo) preservado
- [ ] Detecção de chão funciona corretamente
- [ ] InputModeManager continua funcionando

### 9.2 Funcionalidades Avançadas
- [ ] Bounce em inimigos funciona
- [ ] Plataformas móveis funcionam melhor que antes
- [ ] Animações do Saci funcionam corretamente
- [ ] Performance igual ou melhor que sistema atual

### 9.3 Benefícios Adicionais
- [ ] Interação física com objetos
- [ ] Melhor handling de slopes e rampas
- [ ] Sistema de impulsos funcionando
- [ ] Possibilidade de mecânicas futuras (wall jump, etc.)

## 10. Próximos Passos

1. **Aprovação do Plano**: Revisar e aprovar estratégia
2. **Backup e Branch**: Preparar ambiente de desenvolvimento
3. **Análise do Animator**: Documentar sistema atual de animação
4. **Início da Implementação**: Criar SaciECMController base

---

**Observações Importantes**:
- Este plano é flexível e pode ser ajustado conforme necessário
- Testes constantes são essenciais durante toda a migração
- Manter versão funcional do sistema atual como fallback
- Documentar todas as alterações para facilitar debugging




Pontos a Ajustar

- Integração com InputModeManager : o plano prevê manter e usar; o controlador atual não consulta o InputModeManager para habilitar/desabilitar input conforme modo. Recomenda verificar modo ativo antes de consumir inputs .
- Assinaturas Awake / OnValidate : estão como public override . No ECM, é comum protected virtual na base; se o BaseCharacterController usar protected , o override deve ser protected override para compatibilidade.
- Estado walk : a propriedade privada walk é redundante (não usada em cálculos após a mudança de CalcDesiredVelocity ). Pode ser removida ou usada se houver lógica adicional (ex.: animação/estados específicos de caminhada).
- JumpTrigger : o plano lista como opcional; o controlador não aciona o Trigger . Se o Animator depender do JumpTrigger para entrar em Jump , sugerido acionar junto com isJumping no momento do salto.
- Configuração de componentes: o plano recomenda ajustes no Rigidbody (freeze X/Z, Use Gravity: false ) e CharacterMovement (snap to ground, slope limit, gravity). Esses devem ser conferidos no Inspector; o script não força essas configs.
- Mecânicas avançadas: double jump e bounce on enemy ainda não estão implementados (previstos na Fase 3).
- Root Motion flags: se for usar root motion de rotação/deslocamento, além do RootMotionController , é bom alinhar flags do controlador base (ex.: habilitar rotação por root motion se existir suporte no BaseCharacterController ).
Sugestões Objetivas

- Verificar e ajustar a acessibilidade de Awake e OnValidate para protected override se a base for protected virtual .
- Adicionar verificação de modo no HandleInput() usando InputModeManager (ignorar input quando não estiver em modo de player).
- Remover ou utilizar walk de forma consistente (ex.: para uma flag de estado visual ou auditoria).
- Se o Animator usar JumpTrigger , acionar animator.SetTrigger("JumpTrigger") no início do salto.
- Conferir e aplicar no Inspector as recomendações de Rigidbody e CharacterMovement do plano.
- Planejar a Fase 3: pontos de extensão para double jump e bounce (com CharacterMovement.ApplyImpulse ou equivalente no ECM).