# Sistema de Magnetismo e Espalhamento de Moedas

## Visão Geral

Este documento descreve o sistema de magnetismo e espalhamento de moedas implementado no jogo. O sistema permite que moedas sejam espalhadas quando caixas são quebradas e depois atraídas pelo jogador quando ele se aproxima, criando uma experiência de coleta dinâmica e satisfatória.

## Componentes Principais

### BoxInteractor.cs
- Gerencia a interação com caixas quebráveis
- Controla o processo de quebra, explosão e respawn das caixas
- Chama o `ItemEffectController` para criar e espalhar moedas
- Quando configurado para não reaparecer (`reapareceDepois = false`), destrói o GameObject da caixa após 2 segundos

### ItemEffectController.cs
- Gerencia a criação, animação e espalhamento de itens (moedas)
- Controla a animação de subida e descida dos itens
- Implementa o espalhamento radial dos itens
- Notifica cada moeda quando seu espalhamento foi concluído através do método `EspalhamentoConcluido()`

### CoinPickup.cs
- Controla o comportamento individual de cada moeda
- Gerencia o magnetismo e a coleta de moedas
- Implementa um sistema de atraso para o magnetismo através das variáveis:
  - `tempoEsperaAntesDoMagnetismo`: tempo de espera antes de ativar o magnetismo
  - `magnetismoHabilitado`: indica se o magnetismo está habilitado após o tempo de espera
  - `espalhamentoConcluido`: indica se o espalhamento da moeda foi concluído
  - `ignorarTempoEspera`: permite que moedas colocadas diretamente no mapa ignorem o tempo de espera

### PlayerMagneticField.cs
- Cria um campo magnético ao redor do jogador
- Usa um GameObject filho com um SphereCollider configurado como trigger
- Define a tag "MagneticTrigger" para interagir com as moedas

### PlayerCoinAttractor.cs
- Configura um ponto de atração personalizado para moedas
- Usa um collider com a tag "MagneticTrigger" para detectar moedas próximas
- Define o ponto de atração para cada moeda que entra no campo magnético

## Configurações Importantes

### Em CoinPickup.cs
- `tempoEsperaAntesDoMagnetismo`: Define o tempo (em segundos) que a moeda deve esperar antes de poder ser atraída pelo magnetismo. Valor padrão: 0.6 segundos.
- `ignorarTempoEspera`: Quando ativado, permite que moedas colocadas diretamente no mapa ignorem o tempo de espera e sejam atraídas imediatamente pelo magnetismo.

### Em ItemEffectController.cs
- `raioEspalhamento`: Define o raio do círculo em que as moedas serão espalhadas.
- `tempoAntesDeEspalhar`: Define o tempo de espera antes de iniciar o espalhamento radial.

## Como Funciona

1. Quando uma caixa é quebrada, o `BoxInteractor` chama o `ItemEffectController` para criar moedas.
2. O `ItemEffectController` cria as moedas e inicia a animação de subida e descida.
3. Após o tempo definido em `tempoAntesDeEspalhar`, as moedas são espalhadas radialmente.
4. Quando cada moeda conclui seu movimento de espalhamento, o método `EspalhamentoConcluido()` é chamado.
5. O `CoinPickup` marca a moeda como tendo concluído o espalhamento através da variável `espalhamentoConcluido`.
6. Após o tempo definido em `tempoEsperaAntesDoMagnetismo` e se o espalhamento foi concluído, a moeda ativa o magnetismo (`magnetismoHabilitado = true`).
7. Se a moeda estiver dentro do campo magnético do jogador (trigger com tag "MagneticTrigger"), ela será atraída para o jogador.
8. Quando a moeda chega próxima o suficiente do jogador, ela é coletada automaticamente.

## Dicas de Ajuste

### Para moedas de caixas quebráveis:
- Ajuste `tempoEsperaAntesDoMagnetismo` em `CoinPickup` para controlar quanto tempo as moedas esperam antes de serem atraídas.
- Ajuste `tempoAntesDeEspalhar` em `ItemEffectSettings` para controlar quando as moedas começam a se espalhar.
- Ajuste `raioEspalhamento` em `ItemEffectSettings` para controlar o quão longe as moedas se espalham.

### Para moedas colocadas diretamente no mapa:
- Ative a opção `ignorarTempoEspera` para que essas moedas sejam atraídas imediatamente quando o jogador se aproxima.
- Isso é útil para moedas que não precisam passar pelo processo de espalhamento.

## Solução de Problemas

### Moedas são atraídas muito cedo (antes de completar o espalhamento)
- Verifique se o `espalhamentoConcluido` está sendo corretamente definido como `true` apenas após a conclusão do movimento de espalhamento.
- Aumente o valor de `tempoEsperaAntesDoMagnetismo` em `CoinPickup`.
- Verifique se a opção `ignorarTempoEspera` está desativada para moedas que devem esperar.

### Moedas são atraídas muito tarde ou não são atraídas
- Verifique se o campo magnético do jogador está funcionando corretamente (tag "MagneticTrigger").
- Diminua o valor de `tempoEsperaAntesDoMagnetismo` em `CoinPickup`.
- Verifique se `magnetismoHabilitado` está sendo definido como `true` após o tempo de espera.
- Para moedas colocadas diretamente no mapa, verifique se `ignorarTempoEspera` está ativado.

### Comportamento inconsistente ao quebrar várias caixas rapidamente
- Cada moeda tem seu próprio temporizador e estado de espalhamento.
- O sistema foi atualizado para garantir que cada moeda só seja atraída após concluir seu próprio espalhamento.
- Verifique se o método `EspalhamentoConcluido()` está sendo chamado corretamente para cada moeda após seu movimento de espalhamento.

### Caixas não desaparecem quando configuradas para não reaparecer
- Verifique se `reapareceDepois` está definido como `false` no `BoxInteractor`.
- O sistema foi atualizado para destruir o GameObject da caixa após 2 segundos quando `reapareceDepois` é `false`.