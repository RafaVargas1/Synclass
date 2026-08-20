# Implementation: Alocação/marcação decide pela política do horário (#74)

## Entidades/classes afetadas

- **Domain** (`backend/src/Synclass.Domain/Alocacoes/AlocacaoHorarioService.cs`):
  - `AlocarAsync` já busca `horario` (via `_horarioService.BuscarDoProfessorAsync`)
    antes de checar a política — `GarantirModeloPermiteAlocacaoAsync` deixa de
    ser assíncrono/depender de repositório e passa a receber `Horario` (ou só
    `TipoMarcacao`), decidindo: `Livre` rejeita, `Fixo`/`Hibrido` aceita
    (equivalente exato do `Vago`/`Fixo`/`Hibrido` por Professor de hoje).
  - `MarcarAsync` idem: `GarantirModeloPermiteMarcacaoAsync` passa a decidir
    por `Horario.TipoMarcacao`, **sem** consultar mais se o horário já tem
    atribuição fixa do Professor — `Hibrido` sempre permite marcação livre
    agora (mudança de comportamento intencional do card, AC3).
  - `ListarVagosAsync`/`ParaHorarioVagoSeElegivelAsync`: removem a consulta a
    `ConfiguracaoProfessor` (o `foreach` já itera `Horario`, cada um já
    carrega `TipoMarcacao`); elegibilidade vira `horario.TipoMarcacao is
    TipoMarcacao.Livre or TipoMarcacao.Hibrido && vagasRestantes > 0`.
  - Remove o campo `_configuracoes`/parâmetro `IConfiguracaoProfessorRepository`
    do construtor — nada mais na classe usa depois desta Task (confirmado:
    único uso era ler `ModeloAgendamento`, que sai de cena aqui).
  - `ModeloNaoPermiteAlocacaoException`/`ModeloNaoPermiteMarcacaoLivreException`:
    reaproveitadas (não removidas — mesma função semântica: "a política não
    permite esta ação"), mas o construtor passa a receber `horarioId` em vez
    de `professorId` e a mensagem passa a referenciar o horário
    (`$"O horário {horarioId} não permite atribuição fixa de Alunos."` /
    `"...não permite marcação livre pelo Aluno."`) — mais preciso agora que
    a decisão é por horário, não mais por Professor.
- **Sem mudança de contrato de API**: `AlocacoesHorarioController`/
  `MarcacoesHorarioController` não mudam de assinatura, só o comportamento
  interno do service que já chamam.

## Modelo de dados

Nenhuma mudança de schema — só leitura de `Horario.TipoMarcacao` (coluna já
existe desde a Task #73).

## Edge points (não cobertos por Gherkin)

- Não existe mais "ausência de configuração" como estado possível para
  decidir a política de um horário específico — `TipoMarcacao` é campo
  obrigatório do `Horario` desde a Task #73, então os `if (configuracao is
  null)` defensivos de `GarantirModeloPermiteAlocacaoAsync`/
  `GarantirModeloPermiteMarcacaoAsync`/`ListarVagosAsync` somem (não têm
  mais para onde apontar — um `Horario` sempre tem `TipoMarcacao` válido).
- `PossuiAlocacaoOrigemProfessorAsync` (em `IAlocacaoHorarioRepository`) deixa
  de ser chamado por este service depois da mudança — **não remover o
  método da interface/implementação** ainda sem checar se outra Task/tela já
  depende dele fora deste arquivo (não achado uso externo na reflexão desta
  Task, mas confirme com uma busca antes de apagar; se realmente não houver
  mais nenhum uso, remover é aceitável, só não é o foco desta Task).
- Testes de fumaça hoje montam o cenário definindo `ModeloAgendamento` do
  Professor via `PUT /professores/{professorId}/configuracao/modelo-agendamento`
  antes de cadastrar o horário — o cadastro do horário já exige essa
  configuração existir (regra da issue #7, não muda nesta Task), então o
  `PUT` continua necessário no arranjo do teste, só que agora o valor que
  efetivamente controla o comportamento testado é o `TipoMarcacao` passado
  no `POST /horarios`, não mais o modelo do `PUT`.

## Dependência de outras Tasks

Depende da Task #73 (`Horario.TipoMarcacao`), já mergeada em `main`. Não
migra horários já existentes no banco (isso é a Task #75, que depende desta
para saber o `TipoMarcacao` alvo de cada horário migrado).
