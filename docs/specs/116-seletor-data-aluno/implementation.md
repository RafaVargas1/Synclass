# Implementação: SeletorDeData no fluxo do Aluno (#116)

## Componentes afetados

- `frontend/src/app/aluno/historico-frequencia.tsx`, `frontend/src/app/aluno/valor-devido.tsx`:
  trocam `SeletorDePeriodo` por dois `SeletorDeData` (Início/Fim).
- `frontend/src/components/molecules/SeletorDePeriodo.tsx`: removido se
  ficar órfão após a migração (sem código morto).

## Por que reaproveitar `SeletorDeData` em vez de ajustar `SeletorDePeriodo`

`SeletorDeData` já resolve exatamente esse problema (calendário embutido)
pro Professor — criar/ajustar `SeletorDePeriodo` pra abrir um calendário
duplicaria a mesma lógica que `SeletorDeData` já tem. Reaproveitar é a
opção que já está estabelecida como padrão de code-style do projeto
("sem duplicação de código").

## Contrato de API

Nenhum — o formato do período (`{ inicio, fim }` em `yyyy-MM-dd`) que a
Api já espera não muda; só a forma de capturar essas duas strings do
usuário.

## Testes

Testes de `aluno/historico-frequencia.test.tsx`/`aluno/valor-devido.test.tsx`
que hoje simulam `fireEvent.changeText` nos campos de texto passam a
simular a seleção via `CalendarioMensal` mockado (mesmo padrão já usado
em `professor/[professorId]/valor-devido.test.tsx` para testar
`SeletorDeData`) — reaproveitar esse padrão de mock em vez de inventar um
novo.
