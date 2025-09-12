using UnityEngine;
using DG.Tweening;
using System.Diagnostics; // DOTween namespace

/// <summary>
/// Controla o comportamento de moedas coletáveis, incluindo rotação, magnetismo e efeitos de coleta.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CoinPickup : MonoBehaviour
{
    [Header("Efeitos de Coleta")]
    [Tooltip("Sistema de partículas que será ativado quando a moeda for coletada")]
    public ParticleSystem coletadoEfeito;
    
    [Tooltip("Som que será reproduzido quando a moeda for coletada")]
    public AudioClip somColeta;
    
    [Tooltip("Tempo em segundos antes de destruir o objeto após ser coletado")]
    public float tempoDestruir = 1f;

    [Header("Animação")]
    [Tooltip("Velocidade de rotação em graus por segundo")]
    public float velocidadeRotacao = 180f;

    [Header("Magnetismo")]
    [Tooltip("Velocidade com que a moeda se move em direção ao jogador quando atraída")]
    public float velocidadeMagnetismo = 5f;
    
    [Tooltip("Distância mínima para coletar automaticamente a moeda")]
    public float distanciaMinimaParaColetar = 0.5f;
    
    [Tooltip("Raio de atração magnética da moeda")]
    public float raioAtracao = 3f;
    
    [Tooltip("Tipo de easing aplicado ao movimento de magnetismo")]
    public Ease easeDoMagnetismo = Ease.OutQuad;
    
    [Tooltip("Cor do gizmo que mostra o raio de atração no editor")]
    public Color corGizmo = new Color(0, 1, 1, 0.3f); // Ciano semi-transparente
    
    [Tooltip("Ponto de atração personalizado no jogador (opcional). Se não for definido, usará o transform do jogador")]
    public Transform pontoAtracaoPersonalizado;
    
    [Tooltip("Tempo de espera antes de permitir que a moeda seja atraída pelo magnetismo (em segundos)")]
    public float tempoEsperaAntesDoMagnetismo = 0.6f;
    
    [Tooltip("Se ativado, ignora o tempo de espera e permite que a moeda seja atraída imediatamente pelo magnetismo. Útil para moedas colocadas diretamente no mapa.")]
    public bool ignorarTempoEspera = false;
    
    [Tooltip("Tempo em segundos para desativar a coleta por toque durante a animação de espalhamento")]
    public float tempoDesativarColetaPorToque = 2f;
    
    /// <summary>
    /// Desenha gizmos no editor para visualizar o raio de atração magnética.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = corGizmo;
        Gizmos.DrawSphere(transform.position, raioAtracao);
    }
    
    /// <summary>
    /// Método chamado pelo ItemEffectController quando o espalhamento da moeda é concluído.
    /// Isso garante que o magnetismo só seja ativado após a moeda ter terminado de se espalhar.
    /// </summary>
    public void EspalhamentoConcluido()
    {
        espalhamentoConcluido = true;
        UnityEngine.Debug.Log("Espalhamento concluído para: " + gameObject.name);
        
        // Se a opção de ignorar tempo de espera estiver ativada, habilitar o magnetismo imediatamente
        if (ignorarTempoEspera)
        {
            magnetismoHabilitado = true;
            
            // Se a moeda já estiver dentro de um campo magnético, ativar o magnetismo agora
            if (ultimoTriggerMagnetico != null && ultimoTriggerMagnetico.CompareTag("MagneticTrigger"))
            {
                Transform jogador = ultimoTriggerMagnetico.transform.parent;
                MoverAteJogadorDOTween(jogador);
                UnityEngine.Debug.Log("Magnetismo ativado após espalhamento para: " + gameObject.name);
            }
        }
    }
    
    /// <summary>
    /// Define programaticamente um ponto de atração personalizado para a moeda.
    /// </summary>
    /// <param name="novoPontoAtracao">Transform que será usado como ponto de atração</param>
    public void DefinirPontoAtracaoPersonalizado(Transform novoPontoAtracao)
    {
        pontoAtracaoPersonalizado = novoPontoAtracao;
        
        // Se o magnetismo já estiver ativo, atualiza o ponto de atração
        if (magnetismoAtivo && jogadorAlvo != null)
        {
            pontoAtracao = pontoAtracaoPersonalizado != null ? pontoAtracaoPersonalizado : jogadorAlvo;
        }
    }

    // Componentes e estado
    private AudioSource audioSource;
    private bool coletado = false;
    private Transform jogadorAlvo = null;
    private Transform pontoAtracao = null;
    private bool magnetismoAtivo = false;
    private float tempoDeSpawn;
    private bool magnetismoHabilitado = false;
    private bool espalhamentoConcluido = false;
    private bool coletaPorToqueHabilitada = true; // Controla se a coleta por toque está ativa
    private string rotationTweenId; // ID único da animação de rotação para cancelamento seguro


    /// <summary>
    /// Inicializa a moeda, configurando o AudioSource, o Collider e iniciando a animação de rotação.
    /// </summary>
    private void Start()
    {
        UnityEngine.Debug.Log("=== START CHAMADO ===\n" +
                             "Nome: " + gameObject.name + "\n" +
                             "Posição: " + transform.position + "\n" +
                             "Ativo: " + gameObject.activeInHierarchy + "\n" +
                             "Velocidade Rotação: " + velocidadeRotacao);
        
        // Configurar componentes
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        GetComponent<Collider>().isTrigger = true;
        
        // Registrar o tempo de spawn para controlar o atraso do magnetismo
        tempoDeSpawn = Time.time;
        
        // Desativar coleta por toque temporariamente para permitir o espalhamento
        coletaPorToqueHabilitada = false;
        
        // Se a opção de ignorar tempo de espera estiver ativada, habilitar o magnetismo imediatamente
        // e considerar o espalhamento como concluído
        if (ignorarTempoEspera)
        {
            magnetismoHabilitado = true;
            espalhamentoConcluido = true; // Considerar o espalhamento como concluído quando ignorarTempoEspera está ativado
            UnityEngine.Debug.Log("Moeda criada: " + gameObject.name + ". Magnetismo habilitado imediatamente (ignorando tempo de espera).");
        }
        else
        {
            magnetismoHabilitado = false;
            espalhamentoConcluido = false; // Espalhamento não concluído por padrão
            UnityEngine.Debug.Log("Moeda criada: " + gameObject.name + ". Magnetismo será habilitado em " + tempoEsperaAntesDoMagnetismo + " segundos.");
        }

        UnityEngine.Debug.Log("Prestes a chamar IniciarRotacao() para: " + gameObject.name);
        
        // Iniciar rotação contínua no eixo Y usando DOTween
        IniciarRotacao();
        
        // Reativar coleta por toque após o tempo especificado
        Invoke(nameof(ReativarColetaPorToque), tempoDesativarColetaPorToque);
        
        UnityEngine.Debug.Log("Coleta por toque desativada temporariamente por " + tempoDesativarColetaPorToque + " segundos para: " + gameObject.name);
        
        UnityEngine.Debug.Log("Start concluído para: " + gameObject.name);
    }
    
    /// <summary>
    /// Inicia a animação de rotação contínua da moeda.
    /// </summary>
    private void IniciarRotacao()
    {
        UnityEngine.Debug.Log("=== INICIANDO ROTAÇÃO ===\n" +
                             "Nome: " + gameObject.name + "\n" +
                             "Velocidade Rotação: " + velocidadeRotacao + "\n" +
                             "Transform válido: " + (transform != null) + "\n" +
                             "GameObject ativo: " + gameObject.activeInHierarchy);
        
        // Verificar se o transform ainda é válido antes de criar a animação
        if (transform == null || gameObject == null)
        {
            UnityEngine.Debug.LogError("Transform ou GameObject inválido ao tentar iniciar rotação: " + gameObject?.name);
            return;
        }
        
        // Calcular o tempo necessário para uma rotação completa
        float tempoRotacao = 360f / velocidadeRotacao;
        
        // Criar ID único e específico para esta moeda, independente de qualquer objeto pai
        rotationTweenId = "CoinRotation_" + gameObject.GetInstanceID().ToString() + "_" + System.Guid.NewGuid().ToString().Substring(0, 8);
        
        UnityEngine.Debug.Log("Tempo de rotação calculado: " + tempoRotacao + "s, ID: " + rotationTweenId);
        
        // Aplicar rotação contínua com ID específico e independente
        var tween = transform
            .DORotate(new Vector3(0, 360, 0), tempoRotacao, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart) // rotação infinita
            .SetId(rotationTweenId)
            .SetAutoKill(false); // Evitar que seja morta automaticamente
            
        if (tween != null)
        {
            UnityEngine.Debug.Log("Tween de rotação criado com sucesso para: " + gameObject.name + " com ID: " + rotationTweenId);
        }
        else
        {
            UnityEngine.Debug.LogError("ERRO: Falha ao criar tween de rotação para: " + gameObject.name);
        }
    }

    /// <summary>
    /// Detecta colisões com o jogador ou com o trigger de magnetismo.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Ignorar se já foi coletado
        if (coletado) return;

        // Coleta imediata ao tocar no jogador (apenas se a coleta por toque estiver habilitada)
        if (other.CompareTag("Player") && coletaPorToqueHabilitada)
        {
            UnityEngine.Debug.Log("Moeda coletada por toque direto no jogador: " + gameObject.name);
            Coletar();
        }
        else if (other.CompareTag("Player") && !coletaPorToqueHabilitada)
        {
            UnityEngine.Debug.Log("Coleta por toque ignorada (ainda desabilitada) para: " + gameObject.name);
        }
        // Ativar magnetismo quando entrar no campo de atração, mas apenas se o tempo de espera já passou e o espalhamento foi concluído
        else if (other.CompareTag("MagneticTrigger"))
        {
            // Verificar se este trigger magnético ainda é válido (não foi destruído)
            if (other == null || other.gameObject == null)
            {
                UnityEngine.Debug.Log("Trigger magnético inválido detectado para: " + gameObject.name);
                return;
            }
            
            // Armazenar o trigger magnético para uso posterior
            ultimoTriggerMagnetico = other;
            
            // Se ignorarTempoEspera estiver ativado, ignoramos a verificação de espalhamentoConcluido
            if ((magnetismoHabilitado && espalhamentoConcluido) || ignorarTempoEspera)
            {
                Transform jogador = other.transform.parent; // Pega o transform do jogador (parent do trigger)
                if (jogador != null)
                {
                    MoverAteJogadorDOTween(jogador);
                    UnityEngine.Debug.Log("Entrou no campo de atração e magnetismo está habilitado para: " + gameObject.name + (ignorarTempoEspera ? " (ignorando tempo de espera)" : ""));
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Jogador não encontrado no parent do trigger magnético para: " + gameObject.name);
                }
            }
            else
            {
                string motivo = !magnetismoHabilitado ? "magnetismo ainda não habilitado" : "espalhamento não concluído";
                UnityEngine.Debug.Log("Entrou no campo de atração, mas " + motivo + " para: " + gameObject.name + ". Aguardando " + 
                                     (tempoDeSpawn + tempoEsperaAntesDoMagnetismo - Time.time).ToString("F2") + " segundos.");
            }
        }
    }
    
    /// <summary>
    /// Mantém o registro do último trigger magnético enquanto a moeda estiver dentro dele.
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        // Ignorar se já foi coletado
        if (coletado) return;
        
        // Manter o registro do último trigger magnético
        if (other.CompareTag("MagneticTrigger"))
        {
            ultimoTriggerMagnetico = other;
        }
    }
    
    /// <summary>
    /// Limpa a referência ao trigger magnético quando a moeda sai do campo.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        // Limpar a referência se for o mesmo trigger que estamos rastreando
        if (other == ultimoTriggerMagnetico)
        {
            ultimoTriggerMagnetico = null;
        }
    }

    // Armazena o último trigger magnético que tocou na moeda
    private Collider ultimoTriggerMagnetico = null;
    
    /// <summary>
    /// Atualiza o movimento da moeda em direção ao jogador quando o magnetismo está ativo.
    /// Também verifica se o tempo de espera para habilitar o magnetismo já passou.
    /// </summary>
    private void Update()
    {
        // Verificar se o tempo de espera para habilitar o magnetismo já passou
        // Se ignorarTempoEspera estiver ativado, ignoramos a verificação de espalhamentoConcluido
        if (!magnetismoHabilitado && Time.time >= tempoDeSpawn + tempoEsperaAntesDoMagnetismo && (espalhamentoConcluido || ignorarTempoEspera))
        {
            magnetismoHabilitado = true;
            UnityEngine.Debug.Log("Magnetismo habilitado para: " + gameObject.name);
            
            // Se a moeda já estiver dentro de um campo magnético, ativar o magnetismo agora
            if (ultimoTriggerMagnetico != null && ultimoTriggerMagnetico.CompareTag("MagneticTrigger"))
            {
                Transform jogador = ultimoTriggerMagnetico.transform.parent;
                MoverAteJogadorDOTween(jogador);
                UnityEngine.Debug.Log("Magnetismo ativado após tempo de espera para: " + gameObject.name);
            }
        }
        
        // Se não estiver coletado e o magnetismo estiver ativo
        if (!coletado && magnetismoAtivo && pontoAtracao != null)
        {
            // Verificar se o ponto de atração ainda é válido
            if (pontoAtracao == null || pontoAtracao.gameObject == null)
            {
                UnityEngine.Debug.LogWarning("Ponto de atração inválido para: " + gameObject.name + ". Desativando magnetismo.");
                magnetismoAtivo = false;
                pontoAtracao = null;
                return;
            }
            
            // Calcular a distância atual até o ponto de atração
            float distanciaAtual = Vector3.Distance(transform.position, pontoAtracao.position);
            
            // Se estiver próximo o suficiente, coletar automaticamente
            if (distanciaAtual <= distanciaMinimaParaColetar)
            {
                UnityEngine.Debug.Log("Moeda coletada automaticamente por proximidade: " + gameObject.name + " (distância: " + distanciaAtual.ToString("F2") + ")");
                Coletar();
                return;
            }
            
            // Mover em direção ao ponto de atração
            Vector3 direcao = (pontoAtracao.position - transform.position).normalized;
            transform.position += direcao * velocidadeMagnetismo * Time.deltaTime;
        }
    }
    
    /// <summary>
    /// Move a moeda em direção ao jogador usando DOTween, com efeito de magnetismo.
    /// </summary>
    /// <param name="jogador">Transform do jogador para o qual a moeda deve se mover</param>
    private void MoverAteJogadorDOTween(Transform jogador)
    {
        // Ignorar se já foi coletado
        if (coletado) return;
        
        // Armazenar referência ao jogador
        jogadorAlvo = jogador;
        
        // Determinar o ponto de atração (personalizado ou o próprio jogador)
        if (pontoAtracaoPersonalizado != null)
        {
            pontoAtracao = pontoAtracaoPersonalizado;
        }
        else
        {
            pontoAtracao = jogador;
        }
        
        // Ativar o magnetismo
        magnetismoAtivo = true;
        
        // Log para debug
        UnityEngine.Debug.Log("Magnetismo ativado para: " + gameObject.name + ", usando ponto de atração: " + 
                            (pontoAtracaoPersonalizado != null ? pontoAtracaoPersonalizado.name : "transform do jogador"));
    }

/// <summary>
/// Executa a lógica de coleta da moeda, incluindo efeitos visuais e sonoros.
/// </summary>
private void Coletar()
{
    // Evitar coleta dupla
    if (coletado) 
    {
        UnityEngine.Debug.LogWarning("Tentativa de coleta dupla evitada para: " + gameObject.name);
        return;
    }
    coletado = true;

    // Log detalhado para debug
    UnityEngine.Debug.Log("=== COLETANDO MOEDA ===\n" +
                         "Nome: " + gameObject.name + "\n" +
                         "Posição: " + transform.position + "\n" +
                         "Magnetismo Ativo: " + magnetismoAtivo + "\n" +
                         "Magnetismo Habilitado: " + magnetismoHabilitado + "\n" +
                         "Tempo de Spawn: " + tempoDeSpawn + "\n" +
                         "Tempo Atual: " + Time.time + "\n" +
                         "Tempo desde spawn: " + (Time.time - tempoDeSpawn) + "s\n" +
                         "Espalhamento Concluído: " + espalhamentoConcluido + "\n" +
                         "Ignorar Tempo Espera: " + ignorarTempoEspera + "\n" +
                         "Stack Trace: " + System.Environment.StackTrace);

    // Desativar o magnetismo
    magnetismoAtivo = false;
    jogadorAlvo = null;
    pontoAtracao = null;
    ultimoTriggerMagnetico = null;

    // Cancelar apenas as animações desta moeda específica usando os IDs únicos armazenados
    if (!string.IsNullOrEmpty(rotationTweenId))
    {
        DOTween.Kill(rotationTweenId, true);
        UnityEngine.Debug.Log("Rotação cancelada para: " + gameObject.name + " (ID: " + rotationTweenId + ")");
    }
    
    // Cancelar animação de magnetismo usando padrão antigo como fallback
    string coinId = gameObject.GetInstanceID().ToString();
    DOTween.Kill(coinId + "_magnetism", true);

    // Reproduzir efeito de partículas
    if (coletadoEfeito != null)
        coletadoEfeito.Play();

    // Reproduzir som de coleta
    if (somColeta != null)
    {
        audioSource.clip = somColeta;
        audioSource.Play();
    }

    // Desativar todos os objetos filhos (modelos 3D, etc)
    foreach (Transform child in transform)
        child.gameObject.SetActive(false);

    // Destruir o objeto após o tempo definido
    Destroy(gameObject, tempoDestruir);
}

    /// <summary>
    /// Reativa a coleta por toque após o tempo de espera do espalhamento.
    /// </summary>
    private void ReativarColetaPorToque()
    {
        coletaPorToqueHabilitada = true;
        UnityEngine.Debug.Log("Coleta por toque reativada para: " + gameObject.name);
    }
    
    /// <summary>
    /// Cancela todas as animações DOTween desta moeda quando ela for destruída.
    /// Isso garante que não haja interferência de objetos externos.
    /// </summary>
    private void OnDestroy()
    {
        // Cancelar animação de rotação usando o ID único armazenado
        if (!string.IsNullOrEmpty(rotationTweenId))
        {
            DOTween.Kill(rotationTweenId, true);
            UnityEngine.Debug.Log("OnDestroy: Rotação cancelada para: " + gameObject.name + " (ID: " + rotationTweenId + ")");
        }
        
        // Cancelar qualquer animação de magnetismo como fallback
        string coinId = gameObject.GetInstanceID().ToString();
        DOTween.Kill(coinId + "_magnetism", true);
        
        UnityEngine.Debug.Log("OnDestroy: Todas as animações canceladas para: " + gameObject.name);
    }
}

    


