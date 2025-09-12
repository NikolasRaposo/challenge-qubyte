# Sistema de Moedas com Atração Magnética

Este sistema permite criar moedas coletáveis com efeito de atração magnética para o jogador. As moedas podem ser atraídas para um ponto específico no jogador, em vez de apenas para o pivot central.

## Componentes Principais

### CoinPickup.cs

Este script deve ser adicionado às moedas coletáveis. Ele gerencia:

- Rotação contínua da moeda
- Efeito de atração magnética
- Efeitos de coleta (partículas e som)
- Destruição automática após coleta

#### Configurações Importantes:

- **Velocidade Magnetismo**: Velocidade com que a moeda se move em direção ao jogador
- **Distância Mínima Para Coletar**: Quando a moeda chega a esta distância do ponto de atração, é coletada automaticamente
- **Raio Atração**: Visualizado como uma esfera no editor (apenas para referência visual)
- **Ponto Atração Personalizado**: Transform opcional para definir um ponto específico de atração

### PlayerCoinAttractor.cs

Este script deve ser adicionado ao jogador para criar um campo magnético que atrai moedas. Ele:

- Cria automaticamente um collider trigger com a tag "MagneticTrigger"
- Configura um ponto de atração personalizado para as moedas
- Visualiza o campo magnético e o ponto de atração no editor

## Como Configurar

### Configuração Básica

1. Adicione o componente `CoinPickup` às suas moedas
2. Adicione o componente `PlayerCoinAttractor` ao jogador
3. Configure o raio magnético no `PlayerCoinAttractor`

### Configuração do Ponto de Atração

Existem duas maneiras de configurar o ponto para onde as moedas serão atraídas:

#### Método 1: Usando o Inspector

1. Crie um objeto vazio como filho do jogador (por exemplo, "PontoAtracaoMoedas")
2. Posicione este objeto onde você deseja que as moedas sejam atraídas (por exemplo, na altura do peito do personagem)
3. No componente `PlayerCoinAttractor`, arraste este objeto para o campo "Ponto De Atracao"

#### Método 2: Automático

Se você não definir um ponto de atração no inspector, o script `PlayerCoinAttractor` criará automaticamente um ponto 1.5 unidades acima do jogador.

## Personalização Avançada

### Configuração via Código

Você pode definir programaticamente o ponto de atração para uma moeda específica:

```csharp
// Obter referência à moeda
CoinPickup moeda = GetComponent<CoinPickup>();

// Definir um ponto de atração personalizado
moeda.DefinirPontoAtracaoPersonalizado(seuTransform);
```

### Visualização no Editor

Ambos os scripts incluem visualização de gizmos no editor:

- `CoinPickup`: Mostra uma esfera que representa o raio de atração (apenas visual)
- `PlayerCoinAttractor`: Mostra o campo magnético real e o ponto de atração com uma linha conectando-os

## Dicas de Uso

- Ajuste a "Velocidade Magnetismo" para controlar quão rapidamente as moedas se movem em direção ao jogador
- O campo "Cor Gizmo" permite alterar a cor da visualização no editor para facilitar a identificação
- Para moedas que não devem ser atraídas magneticamente, simplesmente não adicione o componente `CoinPickup`