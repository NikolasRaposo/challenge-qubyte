# Lapidação do Sistema de Fluxo de Entrada

Este documento temporário guia a evolução do `SceneEntryFlowCoordinator` e controladores associados, focando em evitar duplicidades, tornar fases visíveis em runtime e fortalecer orquestração.

## Objetivos

- Tornar a fase atual evidente no Inspector em Play Mode. (Feito)
- Garantir idempotência e evitar chamadas duplicadas. (Feito)
- Centralizar transições por fase e denunciar inconsistências. (Feito — `RequestTransitionTo(Phase)` e aviso de duplicidades no Editor)

## Fases e Estado

- Enum `Phase { StartingGame, EntryUI, Loading, Cinematic, Gameplay }` mantida pela fachada.
- Flags runtime: `isLoading`, `isCinematicPlaying`, `hasHandoffHappened`.
- Painel de Status no Editor exibe fase em verde e flags (somente em Play Mode).

### StartingGame

- Objetivo: executar intro (ex.: logo da empresa) e animações de entrada do Main Menu com input/botões ainda indisponíveis.
- Configuração sugerida: `changeInputContextOnStart = true`, `startInputContext = UI`, `enableUiInteractionsOnStart = false`.
- Finalização: acione `SceneEntryFlowCoordinator.OnStartingGameFinished()` ao término das animações (via Timeline Signal, AnimationEvent ou UnityEvent).
- Efeito: o coordenador chama `EnterUiContextWithFocus(defaultUiButton)` e atualiza `currentPhase = EntryUI` no momento correto.

## Boas Práticas para Evitar Problemas

- Idempotência nos métodos públicos: `ActivateAndPlayCinematic()` checa se já está tocando e aplica cooldown interno.
- Guardas de fase/estado: entradas verificam `isLoading`, `isCinematicPlaying`, `hasHandoffHappened` e saem cedo quando redundantes.
- Janelas de debouncing: `cinematicPlayCooldownMs` evita múltiplas execuções em milissegundos.
- Transições por fase: preferir pedir “transição de fase” em vez de acionar ações diretas quando possível.
- Logs com contexto: `LogWithContext("mensagem")` padroniza saída com a fase atual.

## Padrões Sugeridos

- Fase única como verdade: fachada mantém `currentPhase` como fonte da verdade; múltiplos UnityEvents podem pedir a mesma transição, executada uma única vez.
- Comando em fila (futuro): eventos inserem comandos (`StartLoading`, `PlayCinematic`); a fachada consome e de-dupe por tipo.
- Adaptadores finos: controladores expõem operações mínimas (`Play`, `NotifyEnd`) e a fachada decide sequência.

## Melhorias no Editor

- Sinalização de duplicidade (futuro): no painel “Ativadores do fluxo”, marcar quando múltiplos ativadores apontam para o mesmo método.
- Simulação leve (futuro): botão “Simular sequência de Entry” para testar ordem controlada.
- Painel de estado: já implementado com fase em verde e flags runtime.

## Recomendações de Uso

- Para ações críticas, prefira métodos integradores da fachada: `RequestTransitionTo(Phase)`, `ActivateAndPlayCinematic()`, `OnLoadingFinished()`, `OnCinematicFinished()`.
- Use UnityEvents livremente para cenários criativos; mantenha guardas na fachada.
- Centralize efeitos colaterais na fachada para reduzir lógica espalhada.

## Próximos Passos

- Conectar `OnStartingGameFinished()` por Timeline/AnimationEvent nas intros existentes.
- Conectar `OnCinematicFinished()` à HUD/controle por `UnityEvent` padronizado ou por `PlayableDirector` Signals.