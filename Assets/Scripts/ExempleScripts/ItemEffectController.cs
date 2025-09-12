using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Classe que contém todas as configurações para o efeito de itens.
/// Permite configurar facilmente o comportamento dos itens sem modificar o código.
/// </summary>
[System.Serializable]
public class ItemEffectSettings
{
    [Header("Configurações Básicas")]
    [Tooltip("Quantidade de itens a serem criados")]
    public int quantidade = 1;
    
    [Tooltip("Se verdadeiro, os itens se espalharão em um padrão radial")]
    public bool itensEspalhar = true;
    
    [Header("Animação de Subida")]
    [Tooltip("Altura máxima que os itens sobem antes de cair")]
    public float alturaSobe = 2f;
    
    [Tooltip("Tempo total para subir e descer (em segundos)")]
    public float tempoSobeDesce = 0.5f;
    
    [Header("Configurações de Espalhamento")]
    [Tooltip("Distância máxima que os itens se espalham do centro")]
    public float raioEspalhamento = 2f;
    
    [Tooltip("Tempo de espera antes de iniciar o espalhamento (em segundos)")]
    public float tempoAntesDeEspalhar = 0.05f;
    
    [Tooltip("Velocidade de rotação dos itens (em graus por segundo)")]
    public float velocidadeRotacao = 360f;
    
    [Tooltip("Se verdadeiro, os itens tentarão evitar se sobrepor")]
    public bool evitarSobreposicao = true;
    
    [Tooltip("Distância mínima entre os itens quando evitarSobreposicao está ativado")]
    public float distanciaMinima = 0.5f;
}

/// <summary>
/// Controla o efeito de criação e espalhamento de itens, como moedas ou power-ups.
/// Este script é responsável por criar os itens, animá-los subindo e descendo,
/// e espalhá-los em um padrão radial com variações para evitar sobreposição.
/// </summary>
public class ItemEffectController : MonoBehaviour
{
    [Header("Configurações dos Itens")]
    [Tooltip("Prefab do item que será instanciado")]
    public GameObject itemPrefab;
    
    [Tooltip("Configurações de comportamento dos itens")]
    public ItemEffectSettings configuracoes = new ItemEffectSettings();

    // Lista para rastrear todos os itens criados
    private List<GameObject> itens = new List<GameObject>();
    private System.Random random = new System.Random();

    /// <summary>
    /// Cria os itens, inicia a animação de subida e programa o espalhamento.
    /// Este método é chamado pelo BoxInteractor quando um item deve ser solto.
    /// </summary>
    public void CriarItens()
    {
        // Limpar qualquer item anterior
        itens.Clear();

        // Criar a quantidade especificada de itens
        for (int i = 0; i < configuracoes.quantidade; i++)
        {
            GameObject item = Instantiate(itemPrefab, transform.position, Quaternion.identity);
            
            // Garantir que cada item tenha um nome único para facilitar depuração
            item.name = itemPrefab.name + "_" + System.Guid.NewGuid().ToString().Substring(0, 8);
            
            // Adicionar à lista de itens gerenciados
            itens.Add(item);
            
            // Registrar o tempo de criação no CoinPickup se existir
            CoinPickup coinPickup = item.GetComponent<CoinPickup>();
            if (coinPickup != null)
            {
                // O CoinPickup vai registrar o tempo de spawn no Start()
                // Não precisamos fazer nada adicional aqui
            }
        }

        // Iniciar a animação de subida e descida
        SubirEDescer();

        // Programar o espalhamento após um pequeno atraso
        if (configuracoes.itensEspalhar)
        {
            // Usar um identificador único para cada chamada de espalhamento
            // Isso garante que cada caixa terá seu próprio callback independente
            string uniqueId = "EspalharRadial_" + gameObject.GetInstanceID() + "_" + System.Guid.NewGuid().ToString();
            Debug.Log("Iniciando espalhamento radial para " + itens.Count + " itens com raio " + configuracoes.raioEspalhamento);
            DOVirtual.DelayedCall(configuracoes.tempoAntesDeEspalhar, EspalharRadial)
                .SetId(uniqueId);
        }
    }

    /// <summary>
    /// Aplica a animação de subida e descida a todos os itens criados.
    /// Também inicia a rotação contínua dos itens.
    /// </summary>
    private void SubirEDescer()
    {
        foreach (var item in itens)
        {
            if (item == null) continue;

            // Definir posições inicial e final para a animação
            Vector3 posOriginal = item.transform.position;
            Vector3 posAlvo = posOriginal + Vector3.up * configuracoes.alturaSobe;

            // Criar identificadores únicos para cada animação usando GUID para garantir unicidade
            // Adicionando o ID da instância do GameObject para evitar conflitos entre diferentes controladores
            string uniqueIdSubida = "Subida_" + item.name + "_" + gameObject.GetInstanceID() + "_" + System.Guid.NewGuid().ToString();
            string uniqueIdDescida = "Descida_" + item.name + "_" + gameObject.GetInstanceID() + "_" + System.Guid.NewGuid().ToString();
            
            // Animar o movimento para cima
            item.transform.DOMoveY(posAlvo.y, configuracoes.tempoSobeDesce / 2f)
                .SetEase(Ease.OutSine)
                .SetId(uniqueIdSubida) // Identificador único para animação de subida
                .OnComplete(() =>
                {
                    // Animar o movimento para baixo quando atingir o ponto mais alto
                    item.transform.DOMoveY(posOriginal.y, configuracoes.tempoSobeDesce / 2f)
                        .SetEase(Ease.InSine)
                        .SetId(uniqueIdDescida); // Identificador único para animação de descida
                });

            // Rotação removida - deixar que o próprio prefab da moeda (CoinPickup.cs) gerencie sua rotação
            // Isso evita conflitos entre animações DOTween duplicadas no mesmo transform
        }
        
        // Log para debug
        Debug.Log($"Iniciada animação de subida e descida para {itens.Count} itens com altura {configuracoes.alturaSobe} e tempo {configuracoes.tempoSobeDesce}");
    }

    /// <summary>
    /// Espalha os itens em um padrão radial com variações aleatórias.
    /// Evita sobreposição de itens e colisões com o ambiente.
    /// </summary>
    private void EspalharRadial()
    {
        // Log para debug
        Debug.Log($"Iniciando espalhamento radial para {itens.Count} itens com raio {configuracoes.raioEspalhamento}");
        
        // Verificar se ainda temos itens válidos
        if (itens.Count == 0)
        {
            Debug.LogWarning("Tentativa de espalhar itens, mas a lista está vazia.");
            return;
        }
        
        // Calcular ângulo entre itens para distribuição uniforme
        float anguloEntre = 360f / itens.Count; // Usar o número real de itens, não a configuração
        List<Vector3> posicoesDestino = new List<Vector3>();
        
        // Primeiro, gerar todas as posições de destino
        for (int i = 0; i < itens.Count; i++)
        {
            if (itens[i] == null)
            {
                Debug.LogWarning($"Item {i} é nulo durante o espalhamento.");
                continue;
            }
            
            // Adicionar uma pequena variação aleatória ao ângulo para evitar padrões muito regulares
            float variacaoAngulo = UnityEngine.Random.Range(-10f, 10f);
            float angulo = (i * anguloEntre + variacaoAngulo) * Mathf.Deg2Rad;
            
            // Adicionar uma pequena variação ao raio para evitar que itens fiquem em círculo perfeito
            float raioVariado = configuracoes.raioEspalhamento * UnityEngine.Random.Range(0.8f, 1.2f);
            
            // Calcular direção e posição de destino
            Vector3 dir = new Vector3(Mathf.Cos(angulo), 0, Mathf.Sin(angulo));
            Vector3 origem = itens[i].transform.position;
            Vector3 destino = origem + dir * raioVariado;
            
            // Verificar colisões com o ambiente
            if (Physics.Raycast(origem, dir, out RaycastHit hit, raioVariado))
                destino = hit.point + hit.normal * 0.3f;
                
            // Verificar sobreposição com outros itens se a opção estiver ativada
            if (configuracoes.evitarSobreposicao)
            {
                int tentativas = 0;
                while (EstaProximoDeOutraPosicao(destino, posicoesDestino) && tentativas < 5)
                {
                    // Tentar uma nova direção com uma variação maior
                    variacaoAngulo = UnityEngine.Random.Range(-45f, 45f);
                    angulo = (i * anguloEntre + variacaoAngulo) * Mathf.Deg2Rad;
                    raioVariado = configuracoes.raioEspalhamento * UnityEngine.Random.Range(0.7f, 1.3f);
                    
                    dir = new Vector3(Mathf.Cos(angulo), 0, Mathf.Sin(angulo));
                    destino = origem + dir * raioVariado;
                    
                    if (Physics.Raycast(origem, dir, out hit, raioVariado))
                        destino = hit.point + hit.normal * 0.3f;
                        
                    tentativas++;
                }
            }
            
            // Armazenar a posição de destino e iniciar o movimento
            posicoesDestino.Add(destino);
            
            // Cancelar qualquer animação de movimento anterior para este item usando um ID único
            DOTween.Kill(itens[i].name);
            
            // Armazenar referência ao item atual para uso no callback
            GameObject currentItem = itens[i];
            
            // Criar identificador único para a animação de movimento usando GUID e o ID da instância do GameObject
            // para garantir unicidade mesmo quando várias caixas são quebradas rapidamente
            string uniqueIdMovimento = "Movimento_" + currentItem.name + "_" + gameObject.GetInstanceID() + "_" + System.Guid.NewGuid().ToString();
            
            // Iniciar nova animação de movimento
            currentItem.transform.DOMove(destino, 0.4f)
                .SetEase(Ease.OutQuad)
                .SetId(uniqueIdMovimento)
                .OnComplete(() => {
                    // Notificar que o espalhamento foi concluído, caso o item tenha um CoinPickup
                    if (currentItem != null)
                    {
                        CoinPickup coinPickup = currentItem.GetComponent<CoinPickup>();
                        if (coinPickup != null)
                        {
                            coinPickup.EspalhamentoConcluido();
                            UnityEngine.Debug.Log($"Espalhamento concluído para: {currentItem.name}");
                        }
                    }
                });
        }
        
        // Log para debug
        Debug.Log($"Espalhamento concluído para {posicoesDestino.Count} itens");
    }
    
    /// <summary>
    /// Verifica se uma posição está muito próxima de outras posições já definidas.
    /// Usado para evitar que os itens se sobreponham durante o espalhamento.
    /// </summary>
    /// <param name="posicao">A posição a ser verificada</param>
    /// <param name="posicoes">Lista de posições já definidas</param>
    /// <returns>True se a posição estiver muito próxima de outra, False caso contrário</returns>
    private bool EstaProximoDeOutraPosicao(Vector3 posicao, List<Vector3> posicoes)
    {
        foreach (var pos in posicoes)
        {
            if (Vector3.Distance(posicao, pos) < configuracoes.distanciaMinima)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Cancela todas as animações DOTween relacionadas a este ItemEffectController antes da destruição.
    /// Isso evita erros de acesso a Transform destruído.
    /// </summary>
    private void OnDestroy()
    {
        // Não usar DOTween.Kill(transform, true) pois pode tentar acessar transform destruído
        // Em vez disso, usar apenas IDs específicos para cancelar animações
        
        // Cancelar animações específicas usando IDs únicos se houver itens
        if (itens != null)
        {
            foreach (var item in itens)
            {
                if (item != null && item.transform != null)
                {
                    // Cancelar animações específicas do item com verificação de segurança
                    try
                    {
                        string itemId = item.GetInstanceID().ToString();
                        DOTween.Kill(itemId + "_rotation", true);
                        DOTween.Kill(itemId + "_updown", true);
                        DOTween.Kill(item.name, true);
                        
                        // Cancelar animações de movimento usando o padrão de ID único
                        string uniqueIdMovimento = "Movimento_" + item.name + "_" + gameObject.GetInstanceID();
                        DOTween.Kill(uniqueIdMovimento, true);
                        
                        // Cancelar animações com IDs únicos mais específicos
                        string uniqueIdSubida = "Subida_" + item.name + "_" + gameObject.GetInstanceID();
                        string uniqueIdDescida = "Descida_" + item.name + "_" + gameObject.GetInstanceID();
                        
                        DOTween.Kill(uniqueIdSubida, true);
                        DOTween.Kill(uniqueIdDescida, true);
                        // Rotação removida - agora é gerenciada pelo CoinPickup.cs
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Erro ao cancelar animações para item {item.name}: {e.Message}");
                    }
                }
            }
        }
        
        // Cancelar qualquer animação DOTween usando o ID do GameObject como fallback
        try
        {
            string gameObjectId = gameObject.GetInstanceID().ToString();
            DOTween.Kill(gameObjectId, true);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Erro ao cancelar animações do GameObject: {e.Message}");
        }
        
        Debug.Log("ItemEffectController destruído e animações canceladas: " + gameObject.name);
    }
}
