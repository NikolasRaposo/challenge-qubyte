# Guia de Diagnóstico e Correção — JobTempAlloc

Este documento reúne práticas para entender e corrigir avisos do Unity:

`Internal: JobTempAlloc has allocations that are more than the maximum lifespan of 4 frames old`

Esses avisos indicam que alguma alocação temporária de job (geralmente `Allocator.TempJob` ou `Allocator.Temp`) permaneceu viva além do limite permitido. Nem sempre é crítico, mas tende a sinalizar uso incorreto de containers nativos em jobs, APIs de Mesh, ou pacotes que rodam em segundo plano.

## Quando se preocupar
- Ocasional e difícil de reproduzir: normalmente baixo risco; pode ser ruído do Editor ou de pacotes.
- Frequente e correlacionado com ações específicas (entrar/sair de cena, execução longa de efeitos, geração de malhas): vale investigar — pode impactar performance/memória.

## Diagnóstico rápido
- Recompile e observe se o aviso reaparece logo em seguida ao repetir uma ação específica.
- Verifique o `Profiler` (módulos `Memory` e `Jobs`). Picos de `Temp`/`TempJob` próximos à ação ajudam a focar a investigação.

## Obter callstacks das alocações
Para identificar exatamente o ponto de alocação:

- Inicie o Editor com validação de vazamentos:
  - Windows (exemplo):
    - `"C:\Program Files\Unity\Hub\Editor\<versao>\Editor\Unity.exe" -projectPath "e:\UnityG\challenge-qubyte" -diag-job-temp-memory-leak-validation`
  - Reproduza o caso; o Console deve exibir callstacks para as alocações vazadas.

## Leak detection (Editor)
Habilite relatórios mais detalhados durante o Play no Editor:

```csharp
// Editor-only (colocar em um arquivo dentro de Assets/Editor)
using UnityEditor;
using Unity.Collections;

[InitializeOnLoad]
public static class NativeLeakDetectionBootstrap {
    static NativeLeakDetectionBootstrap() {
        NativeLeakDetection.Mode = NativeLeakDetectionMode.Enabled;
    }
}
```

Observação: isso pode gerar mais logs e impactar um pouco a execução no Editor.

## Causas comuns e correções
- Containers nativos sem Dispose:
  - `NativeArray<T>`, `NativeList<T>`, `TransformAccessArray`, `NativeSlice<T>` etc. devem ser liberados.
  - Padrões corretos:
    - Agendar e liberar depois do job terminar:
      ```csharp
      var arr = new NativeArray<T>(count, Allocator.TempJob);
      var handle = job.Schedule();
      handle.Complete();
      arr.Dispose();
      ```
    - Ou liberar acoplado ao job handle:
      ```csharp
      var arr = new NativeArray<T>(count, Allocator.TempJob);
      var handle = job.Schedule();
      arr.Dispose(handle); // Dispose será executado ao completar o job
      ```
    - `TransformAccessArray`:
      ```csharp
      var taa = new TransformAccessArray(transforms);
      var handle = job.Schedule(taa);
      handle.Complete();
      taa.Dispose();
      ```

- APIs de Mesh que retornam containers nativos:
  - `Mesh.AcquireReadOnlyMeshData(...)` → chamar `meshData.Dispose()` (ou usar `using`).
  - `Mesh.AllocateWritableMeshData(...)` → preferir `Mesh.ApplyAndDisposeWritableMeshData(meshData, mesh, ...)` ou garantir `meshData.Dispose()`.

- Jobs sem `Complete()`:
  - Se o job produz dados para o frame atual, chame `Complete()` antes de avançar mais de 4 frames.

- `Allocator.Temp` vs `Allocator.TempJob`:
  - `Temp` deve durar **no máximo 1 frame**.
  - `TempJob` dura **até 4 frames**; use quando o job atravessa alguns frames, sempre garantindo `Dispose`/`Complete`.

## Padrões no projeto atual
- Scripts do jogo não fazem uso explícito de `Allocator.TempJob`/Jobs. Avisos tendem a vir de pacotes (ex.: efeitos, spline, mesh, partículas) ou de APIs gráficas.
- Há wrappers que usam `NativeArray<T>` em `*CommandBuffer` e `AsyncGPUReadback`:
  - Garanta sempre a correta liberação do `NativeArray<T>` depois de consumir os dados do readback.
  - `AsyncGPUReadbackRequest` em si não requer `Dispose`, mas o `NativeArray<T>` que você materializa a partir do readback requer.

## Checklist de correção
- [ ] Identifique o ponto exato com `-diag-job-temp-memory-leak-validation`.
- [ ] Verifique se todo container nativo `Temp/TempJob` recebe `Dispose`.
- [ ] Garanta `Complete()` para jobs que produzem dados usados imediatamente.
- [ ] Revise uso de APIs de Mesh (`AcquireReadOnlyMeshData`, `AllocateWritableMeshData`).
- [ ] Teste no `Profiler` para confirmar queda nos picos de `Temp/TempJob`.

## Boas práticas gerais
- Centralize a criação/vida de containers nativos (evite criar em funções utilitárias que não controlam o tempo de vida).
- Prefira `FindObjectsSortMode.None` para buscas quando não precisar ordenação (melhor performance e menos trabalho do GC).
- Reinicie o Editor ocasionalmente durante sessões muito longas; alguns pacotes acumulam estado em Editor.

## Template de relatório
Use este modelo ao capturar um stack para facilitar a correção:

```
Contexto: [Ação que dispara o aviso]
Stack (resumido):
1) Método/Classe: ...
2) Origem do pacote/API: ...
Container/Allocator: [NativeArray/NativeList/TransformAccessArray] — [Temp/TempJob]
Hipótese: [falta Dispose / job sem Complete / mesh data não liberado]
Correção proposta: [...]
Status após correção: [Aviso sumiu / diminuiu / persiste]
```

---

Se o aviso começar a aparecer com frequência, capture um exemplo com a flag de diagnóstico e registre um relatório usando o template acima. Com isso, fica rápido direcionar a correção para o ponto exato (seja em código do projeto ou em pacotes).