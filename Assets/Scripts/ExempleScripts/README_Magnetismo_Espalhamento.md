# Sistema de Magnetismo e Espalhamento de Moedas

## Visão Geral

Este sistema gerencia o comportamento de moedas coletáveis no jogo, incluindo:

1. **Espalhamento inicial** - Quando moedas são criadas (por exemplo, ao quebrar uma caixa), elas primeiro se espalham em um padrão radial
2. **Tempo de espera** - Após o espalhamento, há um período de espera antes que o magnetismo seja habilitado
3. **Magnetismo** - Quando habilitado, as moedas são atraídas para o jogador ao entrar no campo magnético

## Componentes Principais

### BoxInteractor.cs
- Gerencia a interação com caixas quebráveis
- Cria o ItemEffectController que gerencia o espalhamento das moedas

### ItemEffectController.cs
- Controla a animação de subida/descida e espalhamento das moedas
- Configura o padrão de espalhamento radial

### CoinPickup.cs
- Gerencia o comportamento individual de cada moeda
- Implementa o sistema de magnetismo com tempo de espera
- Controla a coleta das moedas

### PlayerMagneticField.cs
- Cria um campo magnético ao redor do jogador
- Define o raio de atração das moedas

## Configurações Importantes

### Em CoinPickup.cs

```csharp
// Tempo de espera antes de permitir que a moeda seja atraída pelo magnetismo (em segundos)
public float tempoEsperaAntesDoMagnetismo = 0.6f;

// Se ativado, ignora o tempo de espera e permite que a moeda seja atraída imediatamente pelo magnetismo
public bool ignorarTempoEspera = false;
```

Estas configurações controlam o comportamento do magnetismo:

- `tempoEsperaAntesDoMagnetismo`: Controla quanto tempo a moeda deve esperar após ser criada antes de poder ser atraída pelo magnetismo. Ajuste este valor para controlar o tempo entre o espalhamento e o início do magnetismo.

- `ignorarTempoEspera`: Quando ativado, ignora completamente o tempo de espera e permite que a moeda seja atraída imediatamente pelo magnetismo. Esta opção é especialmente útil para moedas colocadas diretamente no mapa (não criadas por caixas quebráveis).

### Em ItemEffectController.cs

```csharp
// Configurações de Espalhamento
public float raioEspalhamento = 2f;
public float tempoAntesDeEspalhar = 0.05f;
```

Estas configurações controlam o padrão de espalhamento das moedas. Ajuste o `raioEspalhamento` para controlar a distância que as moedas se espalham e `tempoAntesDeEspalhar` para controlar quando o espalhamento começa após a criação das moedas.

## Como Funciona

1. Quando uma caixa é quebrada, o `BoxInteractor` cria um `ItemEffectController`
2. O `ItemEffectController` cria as moedas e inicia a animação de subida/descida
3. Após um pequeno atraso, o `ItemEffectController` espalha as moedas em um padrão radial
4. Cada moeda (`CoinPickup`) registra seu tempo de criação e espera pelo tempo configurado em `tempoEsperaAntesDoMagnetismo`
5. Durante este tempo de espera, mesmo que a moeda entre no campo magnético do jogador, ela não será atraída
6. Após o tempo de espera, o magnetismo é habilitado e a moeda pode ser atraída pelo campo magnético do jogador
7. Se a moeda já estiver dentro do campo magnético quando o tempo de espera terminar, ela será automaticamente atraída

## Dicas de Ajuste

### Para moedas criadas por caixas quebráveis:
- Para um efeito visual mais dramático, aumente o `raioEspalhamento` e o `tempoEsperaAntesDoMagnetismo`
- Para uma jogabilidade mais rápida, diminua esses valores
- Certifique-se de que o `tempoEsperaAntesDoMagnetismo` seja maior que o tempo total da animação de espalhamento para evitar que as moedas sejam atraídas antes de terminarem de se espalhar
- Mantenha `ignorarTempoEspera` desativado para permitir o efeito de espalhamento completo

### Para moedas colocadas diretamente no mapa:
- Ative a opção `ignorarTempoEspera` para que as moedas sejam atraídas imediatamente ao entrar no campo magnético
- Isso é ideal para moedas que não precisam do efeito de espalhamento, como aquelas colocadas em trilhas ou caminhos específicos

## Solução de Problemas

- **As moedas são atraídas muito cedo:** Aumente o valor de `tempoEsperaAntesDoMagnetismo` ou verifique se `ignorarTempoEspera` não está ativado por engano
- **As moedas demoram muito para serem atraídas:** Diminua o valor de `tempoEsperaAntesDoMagnetismo` ou, para resposta imediata, ative `ignorarTempoEspera`
- **As moedas não se espalham o suficiente:** Aumente o valor de `raioEspalhamento` no `ItemEffectSettings`
- **As moedas se espalham demais:** Diminua o valor de `raioEspalhamento` no `ItemEffectSettings`
- **Moedas colocadas no mapa não são atraídas imediatamente:** Verifique se `ignorarTempoEspera` está ativado nessas moedas
- **Moedas de caixas não têm tempo para se espalhar:** Verifique se `ignorarTempoEspera` está desativado nessas moedas