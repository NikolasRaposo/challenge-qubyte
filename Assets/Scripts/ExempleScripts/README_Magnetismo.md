# Sistema de Magnetismo para Moedas

## Visão Geral
Este sistema permite que moedas sejam atraídas automaticamente para o jogador quando ele se aproxima delas, criando um efeito de "magnetismo" que melhora a experiência de coleta.

## Arquivos Incluídos
- `CoinPickup.cs`: Script que deve ser anexado às moedas coletáveis
- `PlayerMagneticField.cs`: Script que deve ser anexado ao jogador para criar o campo magnético

## Como Configurar

### Configuração das Moedas
1. Adicione o componente `CoinPickup` a cada objeto de moeda
2. Configure os parâmetros no Inspector:
   - **Efeitos de Coleta**: Partículas, som e tempo de destruição
   - **Animação**: Velocidade de rotação
   - **Magnetismo**: 
     - `velocidadeMagnetismo`: Velocidade com que a moeda se move em direção ao jogador
     - `raioAtracao`: Raio de visualização no editor (apenas para referência visual)
     - `distanciaMinimaParaColetar`: Distância para coleta automática quando próximo ao jogador

### Configuração do Jogador
1. Adicione o componente `PlayerMagneticField` ao objeto do jogador (mesmo GameObject que contém o `ThirdPersonController`)
2. Configure os parâmetros no Inspector:
   - `raioMagnetico`: Tamanho do campo magnético ao redor do jogador
   - `offsetVertical`: Ajuste da altura do campo magnético

## Como Funciona
1. O script `PlayerMagneticField` cria automaticamente um objeto filho com um `SphereCollider` configurado como trigger
2. Este collider é marcado com a tag "MagneticTrigger"
3. Quando uma moeda entra neste trigger, seu magnetismo é ativado
4. A moeda se move em direção ao jogador usando o método `Update()`
5. Quando a moeda chega próxima o suficiente do jogador (definido por `distanciaMinimaParaColetar`), ela é coletada automaticamente

## Solução de Problemas

Se o magnetismo não estiver funcionando:

1. **Verifique as tags**: Confirme que o objeto filho criado pelo `PlayerMagneticField` tem a tag "MagneticTrigger"
2. **Verifique os colliders**: 
   - As moedas devem ter um collider configurado como trigger
   - O campo magnético do jogador deve ter um SphereCollider como trigger
3. **Verifique o raio**: Ajuste o `raioMagnetico` para garantir que seja grande o suficiente
4. **Verifique os logs**: Observe os logs no console para confirmar que "Campo magnético criado com sucesso!" e "Magnetismo ativado para: [nome da moeda]" aparecem

## Personalização

Você pode personalizar o comportamento do magnetismo ajustando:

- `velocidadeMagnetismo`: Velocidade de atração (valores maiores = movimento mais rápido)
- `raioMagnetico`: Distância de detecção das moedas
- `distanciaMinimaParaColetar`: Distância para coleta automática

## Visualização no Editor

Ambos os scripts incluem gizmos para visualização no editor:
- Esferas azuis ao redor do jogador mostram o campo magnético
- Esferas ciano ao redor das moedas mostram o raio de atração (apenas visual)

Isso facilita o ajuste dos parâmetros no editor da Unity.