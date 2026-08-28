# ADR-0005: checkout do Aluno sempre em marketplace mode (Mercado Pago Connect)

- **Status**: aceita
- **Data**: 2026-08-28

## Contexto

A issue #199 (Aluno paga o valor devido pelo app) precisa decidir para
qual conta do Mercado Pago o dinheiro do Aluno vai quando ele conclui o
Checkout Pro: a conta do Synclass (repassando ao Professor depois, fora
do MP) ou diretamente a conta do Professor que o Aluno está pagando.

A issue #203 (já mergeada) implementou o fluxo de OAuth Connect do
Mercado Pago — cada Professor conecta a própria conta e o Synclass passa
a guardar um `collector_id` por Professor
(`ConexaoMercadoPagoService.ObterCollectorIdAsync`). Isso só faz sentido
se o checkout do Aluno também rotear o pagamento para essa conta — do
contrário a #203 não teria propósito nesta Task.

## Decisão

Toda preferência de checkout criada por
`IGeradorDeCheckout.CriarPreferenciaAsync` inclui `collector_id` no
payload, sempre o `collector_id` do Professor da Matrícula sendo paga
(resolvido via `ConexaoMercadoPagoService.ObterCollectorIdAsync` antes de
criar a preferência). O Synclass nunca aparece como `collector_id` — o
dinheiro do Aluno vai direto para a conta do Professor, não para uma
conta fixa do Synclass a ser repassada depois. `IniciarAsync` rejeita a
tentativa (`ProfessorSemContaConectadaException`, 400) se o Professor
ainda não conectou uma conta — não existe fallback para a conta do
Synclass.

## Consequências

**Positivas**: sem custódia de dinheiro de terceiros pelo Synclass (sem
retenção fiscal/regulatória de intermediário financeiro); o Professor
recebe direto, sem depender de um repasse manual/assíncrono do Synclass;
consistente com o propósito da #203.

**Negativas / riscos aceitos**: o Aluno não consegue pagar um Professor
que ainda não conectou uma conta Mercado Pago — aceito, é o gatilho para
o Professor conectar (mensagem de erro pensada pro usuário, ver
`ProfessorSemContaConectadaException`); o Synclass não tem visibilidade
de settlement direta da transação (fica com o `external_reference`/
webhook de #200 para saber o status, não o dinheiro em si).

## Alternativas consideradas

- **Synclass como `collector_id` fixo, repassando ao Professor depois** —
  descartada: exigiria o Synclass lidar com custódia/repasse de dinheiro
  de terceiros (complexidade regulatória fora de escopo) e tornaria a
  #203 (conexão OAuth por Professor) sem função nesta Task.
