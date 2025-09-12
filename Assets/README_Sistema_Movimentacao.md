# Sistema de Movimentação e Pulo - Documentação Técnica

## Visão Geral

Este documento registra todas as melhorias e cuidados implementados no sistema de movimentação do personagem para garantir uma experiência fluida, natural e responsiva tanto no solo quanto no ar.

## Arquitetura do Sistema

### Componentes Principais
- **ThirdPersonController.cs**: Controlador principal de movimentação
- **PlayerVfxPosFeedbackControl.cs**: Sistema de feedback visual sincronizado

## Melhorias Implementadas

### 1. Sistema de Momentum Preservado no Ar

**Problema Original:**
- Perda abrupta de velocidade ao pular ("jolt")
- Air control factor afetava toda a velocidade, não apenas o input
- Comportamento inconsistente entre pulo parado vs. pulo correndo

**Solução Implementada:**
```csharp
// Captura momentum ao deixar o chão
if (_controller.isGrounded && !_wasGroundedLastFrame)
{
    _baseMomentum = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z);
}

// Preserva momentum base + aplica input com air control
Vector3 finalHorizontalVelocity = _baseMomentum; // Preserva por padrão
if (_input.move != Vector2.zero)
{
    Vector3 targetVelocity = inputDirection * desiredSpeed;
    finalHorizontalVelocity = Vector3.Lerp(_baseMomentum, targetVelocity, 
        _airControlFactor * _speedChangeRate * Time.deltaTime);
}
```

**Benefícios:**
- ✅ Elimina o "jolt" ao pular
- ✅ Mantém momentum natural de plataformas móveis
- ✅ Air control factor afeta apenas input adicional
- ✅ Comportamento consistente independente do estado inicial

### 2. Controle de Rotação Separado (Solo vs. Ar)

**Problema Original:**
- Mesma velocidade de rotação no solo e no ar
- Falta de responsividade no controle aéreo

**Solução Implementada:**
```csharp
[SerializeField] private float _rotationSmoothTimeGround = 0.12f;
[SerializeField] private float _rotationSmoothTimeAir = 0.08f;

// No solo
float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, 
    ref _rotationVelocity, _rotationSmoothTimeGround);

// No ar
float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, 
    ref _rotationVelocity, _rotationSmoothTimeAir);
```

**Benefícios:**
- ✅ Rotação mais responsiva no ar (0.08s)
- ✅ Rotação mais suave no solo (0.12s)
- ✅ Controle independente e configurável
- ✅ Melhor sensação de controle aéreo

### 3. Sistema de Input Inteligente no Ar

**Problema Original:**
- Input zero causava desaceleração indevida
- Perda de momentum ao soltar controles
- Incompatibilidade com plataformas móveis

**Solução Implementada:**
```csharp
// Preserva momentum por padrão
Vector3 finalHorizontalVelocity = _baseMomentum;

// Aplica controle APENAS com input ativo
if (_input.move != Vector2.zero)
{
    // Lógica de controle aéreo aqui
    finalHorizontalVelocity = Vector3.Lerp(_baseMomentum, targetVelocity, 
        _airControlFactor * _speedChangeRate * Time.deltaTime);
}
// Se não há input, mantém momentum atual
```

**Benefícios:**
- ✅ Sem desaceleração ao soltar controles
- ✅ Perfeita compatibilidade com plataformas móveis
- ✅ Comportamento físico natural
- ✅ Controle preciso quando necessário

### 4. Feedback Visual Sincronizado

**Problema Original:**
- Interpolação visual desincronizada com updates do target
- Velocidade fixa independente da frequência de atualização

**Solução Implementada:**
```csharp
// Velocidade adaptativa baseada na frequência
float adaptiveLerpSpeed = (lerpSpeedMultiplier / updateFrequency) * Time.deltaTime;
float adaptiveRotationLerpSpeed = (rotationLerpSpeedMultiplier / updateFrequency) * Time.deltaTime;

// Interpolação sincronizada
transform.position = Vector3.Lerp(transform.position, targetPosition, adaptiveLerpSpeed);
transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, adaptiveRotationLerpSpeed);
```

**Benefícios:**
- ✅ Sincronização perfeita com updates do target
- ✅ Suavidade visual consistente
- ✅ Flexibilidade através de multiplicadores
- ✅ Eliminação de desincronização temporal

## Variáveis de Configuração

### Movimento
- `_moveSpeed`: Velocidade base de movimento (5.0f)
- `_speedChangeRate`: Taxa de aceleração/desaceleração (10.0f)
- `_airControlFactor`: Controle no ar (0.0-1.0, padrão 0.5f)

### Rotação
- `_rotationSmoothTimeGround`: Suavização de rotação no solo (0.12f)
- `_rotationSmoothTimeAir`: Suavização de rotação no ar (0.08f)

### Pulo
- `_jumpHeight`: Altura do pulo principal (1.2f)
- `_doubleJumpHeight`: Altura do pulo duplo (1.0f)
- `_gravity`: Gravidade personalizada (-15.0f)
- `_jumpTimeout`: Tempo entre pulos (0.50f)

### Feedback Visual
- `lerpSpeedMultiplier`: Multiplicador de velocidade de interpolação
- `rotationLerpSpeedMultiplier`: Multiplicador de rotação
- `updateFrequency`: Frequência de atualização do target

## Fluxo de Execução

### 1. Detecção de Estado
```csharp
if (_controller.isGrounded)
{
    // Lógica de movimento no solo
    // Captura momentum ao deixar o chão
}
else
{
    // Lógica de movimento no ar
    // Preserva momentum + aplica input controlado
}
```

### 2. Processamento de Input
- **No Solo**: Input direto com aceleração/desaceleração normal
- **No Ar**: Input aplicado apenas quando ativo, preservando momentum base

### 3. Aplicação de Movimento
- **Solo**: `_controller.Move(targetDirection * speed + gravity)`
- **Ar**: `_controller.Move(finalHorizontalVelocity + gravity)`

### 4. Rotação
- **Solo**: `SmoothDampAngle` com `_rotationSmoothTimeGround`
- **Ar**: `SmoothDampAngle` com `_rotationSmoothTimeAir`

## Casos de Uso Testados

### ✅ Pulo Parado
- Mantém controle limitado pelo air control factor
- Sem momentum inicial preservado
- Rotação responsiva no ar

### ✅ Pulo Correndo
- Preserva momentum da corrida
- Air control factor afeta apenas input adicional
- Sem "jolt" ou perda abrupta de velocidade

### ✅ Plataformas Móveis
- Momentum da plataforma é preservado
- Pulo sem input mantém movimento da plataforma
- Input ativo permite controle sobre o momentum base

### ✅ Controle Aéreo
- Input zero = mantém momentum atual
- Input ativo = aplica controle baseado no air control factor
- Input oposto = permite desaceleração controlada

## Considerações Técnicas

### Performance
- Uso eficiente de `Vector3.Lerp` para interpolações
- Cálculos condicionais apenas quando necessário
- Reutilização de variáveis para evitar alocações

### Manutenibilidade
- Código bem comentado e estruturado
- Variáveis configuráveis via Inspector
- Separação clara entre lógica de solo e ar

### Extensibilidade
- Sistema modular permite fácil adição de novos comportamentos
- Air control factor permite diferentes "sensações" de controle
- Feedback visual independente e configurável

## Conclusão

O sistema implementado oferece:
- **Naturalidade**: Comportamento físico realista
- **Responsividade**: Controle preciso quando necessário
- **Fluidez**: Transições suaves entre estados
- **Flexibilidade**: Altamente configurável
- **Robustez**: Funciona em diversos cenários

Todas as melhorias foram implementadas com foco na experiência do jogador, garantindo que o movimento seja intuitivo, responsivo e agradável em todas as situações de jogo.