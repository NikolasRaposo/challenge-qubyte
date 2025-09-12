using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using ThirdParty.StarterAssets.ThirdPersonController.Scripts;

/// <summary>
/// Controla o comportamento de caixas interativas no jogo.
/// Permite configurar diferentes tipos de interação como quebrar, soltar itens,
/// funcionar como trampolim, explodir, entre outros.
/// </summary>
public class BoxInteractor : MonoBehaviour
{
    [Header("Configurações Gerais")]
    public bool podeInteragirComPulo = true;
    public bool podeInteragirComAtaque = false;
    public bool apenasUmaVez = true;

    [Header("Ações da Caixa")]
    public bool quebraAoInteragir = true;
    public bool someAoInteragir = false;
    public bool reapareceDepois = false;
    public float tempoParaReaparecer = 3f;

    [Header("Soltar Item")]
    public bool soltaItem = false;
    public GameObject itemPrefab;
    public Transform pontoSpawn;
    
    [Header("Configurações de Efeito dos Itens")]
    public ItemEffectSettings configuracoesItens = new ItemEffectSettings();

    [Header("feito de Explosão")]
    public bool explodeAoQuebrar = false;
    public float forcaExplosao = 300f;
    public GameObject prefabPedacos;

    [Header("Trampolim")]
    public bool funcionaComoTrampolim = false;
    public float forcaTrampolim = 10f;
    [Tooltip("Se marcado, usará um componente TrampolimController para efeitos avançados de trampolim")]
    public bool usarControladorTrampolim = false;
    [Tooltip("Referência opcional para um TrampolimController personalizado. Se não for definido, será criado automaticamente")]
    public TrampolimController trampolimPersonalizado;

    [Header("Feedback Visual")]
    public bool feedbackVisual = true;
    public float tremorIntensidade = 0.05f;
    public float tremorDuracao = 0.3f;

    private bool interagida = false;
    private Vector3 escalaOriginal;
    private Renderer rend;
    private Collider col;

    /// <summary>
    /// Inicializa os componentes e armazena valores originais necessários para o funcionamento.
    /// </summary>
    void Start()
    {
        escalaOriginal = transform.localScale;
        rend = GetComponent<Renderer>();
        col = GetComponent<Collider>();
    }

    /// <summary>
    /// Método principal que processa a interação com a caixa.
    /// Executa as ações configuradas como tremor visual, soltar itens, trampolim, etc.
    /// </summary>
    /// <param name="interagidor">Transform do objeto que está interagindo com a caixa</param>
    public void Interagir(Transform interagidor)
    {
        // Verificar se a caixa já foi interagida (caso apenasUmaVez esteja ativado)
        if (apenasUmaVez && interagida) return;
        interagida = true;

        // Executar feedback visual se configurado
        if (feedbackVisual)
            TremorVisual();

        // Soltar itens se configurado
        if (soltaItem && itemPrefab != null)
            SoltarComEfeito();

        // Aplicar efeito de trampolim se configurado
        if (funcionaComoTrampolim && interagidor != null)
            AplicarTrampolim(interagidor);

        // Executar explosão se configurado
        if (explodeAoQuebrar)
            Explodir();

        // Quebrar ou sumir com a caixa conforme configuração
        if (quebraAoInteragir)
            StartCoroutine(Quebrar());
        else if (someAoInteragir)
            StartCoroutine(Sumir());
    }
    
    // ... (O resto das funções como TremorVisual, Explodir, etc. continuam iguais) ...

    /// <summary>
    /// Aplica força para cima no jogador, criando o efeito de trampolim.
    /// Se configurado, usa o TrampolimController para efeitos avançados.
    /// </summary>
    /// <param name="alvo">Transform do objeto que receberá a força</param>
    void AplicarTrampolim(Transform alvo)
    {
        // Se estiver configurado para usar o controlador de trampolim
        if (usarControladorTrampolim)
        {
            // Verificar se já existe um controlador de trampolim personalizado
            TrampolimController trampolim = trampolimPersonalizado;
            
            // Se não houver um controlador personalizado, criar um temporário
            if (trampolim == null)
            {
                // Criar um controlador temporário com as configurações básicas
                trampolim = gameObject.AddComponent<TrampolimController>();
                trampolim.forcaImpulso = forcaTrampolim;
                trampolim.animacaoVisual = true;
                trampolim.usarSom = false; // Para evitar duplicação de som com o feedback da caixa
                
                // Destruir o componente após o uso para não interferir com outras interações
                Destroy(trampolim, 1f);
            }
            
            // Ativar o trampolim com o objeto alvo
            trampolim.AtivarTrampolim(alvo);
        }
        // Caso contrário, usar o comportamento simples original
        else
        {
            // Procurar pelo componente ThirdPersonController no objeto alvo
            if (alvo.TryGetComponent(out ThirdPersonController player))
            {
                // Aplicar a força configurada usando o método do controlador
                player.ApplyUpwardForce(forcaTrampolim);
            }
        }
    }

    /// <summary>
    /// Faz a caixa reaparecer após ter sido quebrada ou sumida.
    /// Inclui animação de escala e tremor para feedback visual.
    /// </summary>
    void Reaparecer()
    {
        // Resetar estado
        interagida = false;
        rend.enabled = true;
        col.enabled = true;
        transform.localScale = Vector3.zero;

        // Animar o reaparecimento com efeito de escala e tremor
        DOTween.Sequence()
            .Append(transform.DOScale(escalaOriginal, 0.5f).SetEase(Ease.OutBack))
            .Join(transform.DOShakePosition(0.3f, 0.05f));
    }

    // --- Funções inalteradas ---

    /// <summary>
    /// Aplica efeito visual de tremor na caixa quando interagida.
    /// </summary>
    void TremorVisual()
    {
        // Usar IDs únicos para as animações de tremor para permitir cancelamento seguro
        string boxId = gameObject.GetInstanceID().ToString();
        
        // Animar tremor de posição e rotação usando DOTween com IDs únicos
        transform.DOShakePosition(tremorDuracao, tremorIntensidade)
            .SetId("tremor_pos_" + boxId);
        transform.DOShakeRotation(tremorDuracao, new Vector3(5f, 5f, 5f))
            .SetId("tremor_rot_" + boxId);
    }

    /// <summary>
    /// Cria e configura o efeito de soltar itens quando a caixa é interagida.
    /// Utiliza o ItemEffectController para gerenciar os itens soltos.
    /// </summary>
    void SoltarComEfeito()
    {
        // Determinar posição de spawn dos itens
        Vector3 spawnPosition = pontoSpawn ? pontoSpawn.position : transform.position + Vector3.up;
        
        // Criar objeto para controlar o efeito com um nome único para cada caixa
        string uniqueId = "ItemEffect_" + System.Guid.NewGuid().ToString().Substring(0, 8);
        GameObject efeito = new GameObject(uniqueId);
        efeito.transform.position = spawnPosition;
        
        // Adicionar e configurar o controlador de efeito
        ItemEffectController effect = efeito.AddComponent<ItemEffectController>();
        effect.itemPrefab = itemPrefab;
        
        // Transferir todas as configurações para o controlador de efeito
        effect.configuracoes = configuracoesItens;
        
        // Iniciar a criação dos itens
        effect.CriarItens();

        // Limpar o objeto de efeito após um tempo
        Destroy(efeito, 5f);
    }
    
    /// <summary>
    /// Cria o efeito de explosão, instanciando fragmentos e aplicando força.
    /// </summary>
    void Explodir()
    {
        // Instanciar o prefab de pedaços se estiver configurado
        if (prefabPedacos != null)
        {
            // Criar os fragmentos na posição da caixa
            GameObject fragmentos = Instantiate(prefabPedacos, transform.position, transform.rotation);
            
            // Aplicar força de explosão em todos os rigidbodies dos fragmentos
            foreach (var rb in fragmentos.GetComponentsInChildren<Rigidbody>())
            {
                rb.AddExplosionForce(forcaExplosao, transform.position, 2f);
            }
            
            // Se não vai reaparecer, destruir os fragmentos após um tempo
            if (!reapareceDepois)
                Destroy(fragmentos, 5f);
        }
        
        // Desativar componentes visuais e de colisão imediatamente
        rend.enabled = false;
        col.enabled = false;

        // Programar o reaparecimento se configurado
        if (reapareceDepois)
            Invoke(nameof(Reaparecer), tempoParaReaparecer);
        // Se não vai reaparecer, aguardar tempo suficiente para animações terminarem antes de destruir
        else {
            Debug.Log("Preparando destruição da caixa: " + gameObject.name);
            StartCoroutine(DestruirAposAnimacoes());
        }
    }

    /// <summary>
    /// Coroutine que controla o efeito de quebrar a caixa após um pequeno delay.
    /// </summary>
    IEnumerator Quebrar()
    {
        // Pequeno delay para sincronizar com outros efeitos
        yield return new WaitForSeconds(0.2f);
        
        // Desativar componentes visuais e de colisão imediatamente
        rend.enabled = false;
        col.enabled = false;

        // Programar o reaparecimento se configurado
        if (reapareceDepois)
            Invoke(nameof(Reaparecer), tempoParaReaparecer);
        // Se não vai reaparecer, aguardar tempo suficiente para animações terminarem antes de destruir
        else {
            Debug.Log("Preparando destruição da caixa (Quebrar): " + gameObject.name);
            StartCoroutine(DestruirAposAnimacoes());
        }
    }

    /// <summary>
    /// Coroutine que controla o efeito de fazer a caixa sumir após um pequeno delay.
    /// </summary>
    IEnumerator Sumir()
    {
        // Pequeno delay para sincronizar com outros efeitos
        yield return new WaitForSeconds(0.2f);
        
        // Desativar componentes visuais e de colisão imediatamente
        rend.enabled = false;
        col.enabled = false;

        // Programar o reaparecimento se configurado
        if (reapareceDepois)
            Invoke(nameof(Reaparecer), tempoParaReaparecer);
        // Se não vai reaparecer, aguardar tempo suficiente para animações terminarem antes de destruir
        else {
            Debug.Log("Preparando destruição da caixa (Sumir): " + gameObject.name);
            StartCoroutine(DestruirAposAnimacoes());
        }
    }

    /// <summary>
    /// Coroutine que aguarda tempo suficiente para todas as animações DOTween terminarem
    /// antes de destruir o GameObject, evitando erros de acesso a Transform destruído.
    /// </summary>
    IEnumerator DestruirAposAnimacoes()
    {
        // Aguardar tempo suficiente para:
        // - Animações de subida/descida dos itens (configuracoes.tempoSobeDesce)
        // - Tempo antes do espalhamento (configuracoes.tempoAntesDeEspalhar)
        // - Animações de movimento radial (0.4f)
        // - Margem de segurança adicional
        float tempoEspera = Mathf.Max(5f, configuracoesItens.tempoSobeDesce + configuracoesItens.tempoAntesDeEspalhar + 1f);
        
        Debug.Log($"Aguardando {tempoEspera} segundos para destruir caixa: {gameObject.name}");
        yield return new WaitForSeconds(tempoEspera);
        
        // Verificação final: cancelar animações DOTween usando IDs específicos em vez do transform
        // Evita tentar acessar o transform que pode estar sendo destruído
        string boxId = gameObject.GetInstanceID().ToString();
        DOTween.Kill(boxId, true);
        
        // Cancelar também possíveis animações de tremor que podem estar rodando
        DOTween.Kill("tremor_pos_" + boxId, true);
        DOTween.Kill("tremor_rot_" + boxId, true);
        
        Debug.Log("Destruindo caixa após aguardar animações: " + gameObject.name);
        Destroy(gameObject);
    }
}