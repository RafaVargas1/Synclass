# Requisitos funcionais (backlog)

> **Status: não implementado.** Este documento existe apenas para dimensionar
> a complexidade da fundação técnica descrita em
> [`docs/spec/architecture.md`](../spec/architecture.md). Cada item abaixo vira
> uma issue no GitHub (label `funcional`) para ser priorizada e implementada em
> etapas futuras, seguindo TDD/SDD — não fazem parte desta entrega de fundação.

## Contas e convites

1. Um usuário pode se cadastrar como **Professor**.
1.1. Um usuário com identidade plena (item 1 ou item 2) pode se autenticar
     (login) na plataforma usando um código de uso único enviado ao seu
     contato de cadastro. Aluno provisório (item 3) não se autentica, pois
     não tem contato de autenticação.
2. Um Professor pode convidar um Aluno via WhatsApp; o Aluno pode baixar o app
   e completar o cadastro para se tornar usuário pleno.
3. Um Professor pode cadastrar um Aluno "provisório" (sem cadastro completo),
   identificado apenas por nome + um identificador único (id e número), sem
   nunca precisar completar o onboarding.
4. Um usuário pode acumular os papéis de **Professor** e **Aluno**
   simultaneamente.
5. Um usuário pode ser Aluno de vários Professores diferentes.

## Horários

6. Um Professor pode cadastrar horários disponíveis para aula, escolhendo a
   duração de cada aula.
7. Um Professor pode escolher o modelo de agendamento do seu sistema:
   - **Horário vago**: o Aluno marca livremente em um horário disponível.
   - **Horário fixo**: o Professor cadastra o horário de cada Aluno.
   - **Híbrido**: horários fixos são cadastrados primeiro; os horários
     restantes ficam disponíveis para marcação livre.
8. Um Professor pode colocar um Aluno em um horário específico.
9. Um Aluno pode marcar uma aula em um horário vago, caso o Professor permita.
10. Um Aluno pode desmarcar uma aula com até X minutos de antecedência — esse
    valor de X é configurado pelo Professor.
10.1. Um Professor pode limitar a quantidade de Alunos por horário, com no
    mínimo 1 e no máximo N (permitindo aulas individuais ou em grupo).
10.2. **(Backlog futuro, fora do MVP — sem issue ainda.)** Um Aluno pode ser
    notificado quando o Professor altera um horário ao qual ele está
    vinculado. Por decisão explícita, o item 6 mantém horários imutáveis
    após criação (remover e recriar, sem edição), então esta notificação só
    volta a fazer sentido se essa regra mudar no futuro.

## Cobrança

11. Um Professor pode criar regras de cobrança flexíveis, por exemplo:
    - Valor por aula, variando conforme a frequência semanal contratada
      (ex: 3x/semana tem preço de aula diferente de 2x/semana).
    - Valor fixo mensal para quem frequenta N vezes por mês.
    - Valor fixo por aula, independente da quantidade.
    - (regra deve ser extensível para outros modelos além destes).
12. Um Professor pode ver quanto cada Aluno deve pagar, de acordo com a regra
    de cobrança aplicada a ele.
13. Um Aluno pode ver quanto tem que pagar no mês.

## Frequência

14. Um Professor pode registrar a frequência de cada aula, marcando quais
    Alunos estiveram presentes ou ausentes.
15. Um Aluno pode confirmar sua presença em uma aula.
16. Um Aluno pode acompanhar seu próprio histórico de frequência.

## Notas de modelagem para etapas futuras

- O modelo de regra de cobrança (item 11) deve ser desenhado de forma
  polimórfica/estratégica (ex: `Strategy` pattern no Domain) para suportar
  novos tipos de regra sem reescrever as existentes.
- A relação Aluno↔Professor é N:N (item 5), então identidade de usuário e
  "matrícula" (vínculo com um Professor específico, com sua própria regra de
  cobrança e horários) são conceitos separados no domínio.
- Alunos "provisórios" (item 3) precisam de um identificador estável que não
  dependa de cadastro completo — considerar isso no desenho da entidade
  `Aluno`/`Vinculo` desde já para não exigir migration destrutiva depois.
- Um "horário" (item 6) é um template recorrente (dia da semana + hora +
  duração), não uma ocorrência datada. Os itens 10, 14, 15 e 16 operam sobre
  uma "aula" — a ocorrência de um horário numa data específica (ex: terça
  19/08). Essa ocorrência não precisa ser pré-gerada: é instanciada sob
  demanda na primeira vez que algo a referencia (cancelamento, confirmação
  de presença ou registro de frequência), identificada pelo par
  (horário, data). Evita job de geração e problema de aulas obsoletas caso
  o horário seja removido e recriado.
