# Regras de negócio transversais

Este documento cobre **invariantes que atravessam mais de uma issue** — a
regra de negócio específica de uma Task (critérios de aceite Gherkin,
edge points) continua vivendo no card do GitHub e em
`docs/specs/<n>-<slug>/task.md`. Aqui só entra o que qualquer agente
(Claude, DeepSeek, outro) precisa saber *antes* de tocar numa área do
domínio, porque outra Task já decidiu e depende disso.

## Identidade de usuário

- Um usuário é identificado de forma única por **contato** (e-mail ou
  telefone normalizado) — nunca crie um segundo registro de usuário para
  o mesmo contato, mesmo entrando por caminhos diferentes (cadastro de
  Professor, cadastro de Aluno, convite, login). Ver issue #1 (cadastro de
  Professor) e issue #61 (Aluno se cadastra independente).
- Um usuário pode acumular os papéis de **Professor** e **Aluno**
  simultaneamente (item 4 de `requisitos-funcionais.md`) — quando um
  cadastro reaproveita uma identidade existente com outro papel, adicione
  o papel novo à mesma conta em vez de duplicar. A troca entre "modo
  Professor" e "modo Aluno" é feita dentro do app (`AlternadorDePapel.tsx`),
  nunca por login/conta separados.

## Identidade ≠ matrícula

- **Identidade** (o usuário, seus papéis, seu contato) e **matrícula**
  (o vínculo com um Professor específico, com sua própria regra de
  cobrança e horários) são conceitos separados no domínio — a relação
  Aluno↔Professor é N:N (item 5 de `requisitos-funcionais.md`). Um Aluno
  pode existir sem nenhuma matrícula ainda (cadastro independente, issue
  #61) e pode ter matrícula com mais de um Professor.
- Alunos "provisórios" (item 3, cadastrados pelo Professor sem
  onboarding completo) precisam de um identificador estável que não
  dependa de cadastro completo — todo Aluno (provisório ou não) recebe um
  identificador único e human-readable na criação (issue #70), gravado na
  tabela `Aluno`, imutável depois de criado.

## Convites

- Um convite (`Convite.cs`) é de **uso único**: `MarcarUsado()` rejeita
  reuso (`ConviteInvalidoException`) e expiração
  (`ConviteExpiradoException`). Token longo (link) e código curto de 5
  dígitos (issue #62) são **dois caminhos de resgate do mesmo convite**,
  não dois recursos independentes — usar qualquer um dos dois invalida o
  outro.
- Unicidade do código de 5 dígitos vale só entre convites ainda válidos
  (não usados, não expirados) — um código pode ser reaproveitado assim que
  o convite anterior que o usava é finalizado.

## Horários e política de marcação

- Um "horário" é um template recorrente (dia da semana + hora + duração),
  não uma ocorrência datada. Uma "aula" é a ocorrência de um horário numa
  data específica — instanciada sob demanda na primeira vez que algo a
  referencia (cancelamento, confirmação de presença, registro de
  frequência), nunca pré-gerada.
- Desde o épico #72, a política de marcação (Livre/Fixo/Híbrido) é uma
  configuração **por horário**, definida no cadastro — não existe mais um
  "modelo de agendamento" único por Professor (a antiga
  `ConfiguracaoProfessor.ModeloAgendamento`, issue #7, foi substituída).
  `Livre`: só o Aluno se marca livremente. `Fixo`: só o Professor atribui.
  `Híbrido`: aceita as duas coisas ao mesmo tempo, até a capacidade
  (`LimiteAlunos`) do horário esgotar.
- Horários mantêm-se imutáveis após criação (remover e recriar, sem
  edição) — decisão explícita, ver item 6 de `requisitos-funcionais.md`.

## Cobrança e frequência

- Regra de cobrança (item 11) é modelada de forma polimórfica/estratégica
  (Strategy pattern no Domain) para suportar novos tipos sem reescrever os
  existentes — nunca faça um `switch` crescente no lugar de uma nova
  estratégia.
- Não adicione um estado de "sucesso" genérico em UI enquanto nenhuma tela
  precisar dele (ver `docs/spec/design-system.md#cor`) — mesma lógica se
  aplica a estados de domínio: não crie um enum/flag novo antecipando uma
  necessidade que nenhuma Task concreta pediu ainda.
