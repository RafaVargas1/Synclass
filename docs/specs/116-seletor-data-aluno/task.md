# Task: Aluno usa calendário (SeletorDeData) em vez de digitar data (#116)

Card: https://github.com/RafaVargas1/Synclass/issues/116

Precedente já resolvido: `frontend/src/app/professor/[professorId]/valor-devido.tsx`
usa dois `SeletorDeData` (Início/Fim, `frontend/src/components/molecules/SeletorDeData.tsx`)
lado a lado, sem botão "Consultar" manual — a consulta reage à mudança de
`inicio`/`fim` (via `useEffect`, ver o hook de estado dessa tela). Migrar
as duas telas do Aluno pro mesmo padrão, em vez de manter
`SeletorDePeriodo` (dois `Input` de texto livre + botão "Consultar").

## Ordem de execução

- [ ] Teste de componente: `aluno/historico-frequencia.tsx` — seleciona início/fim via `SeletorDeData`/`CalendarioMensal` (mock), sem simular digitação de texto; a consulta dispara ao selecionar as duas datas
- [ ] Implementação: `aluno/historico-frequencia.tsx` — troca `SeletorDePeriodo` por dois `SeletorDeData` (labels "Início"/"Fim"), removendo o botão "Consultar" explícito se a tela adotar o padrão reativo do Professor (confirmar olhando `useConsultaHistoricoFrequenciaDoAluno`/hook equivalente — ajustar pra disparar a consulta quando `inicio`/`fim` mudarem, mesmo padrão da tela do Professor)
- [ ] Teste de componente: `aluno/valor-devido.tsx` — mesmo padrão de teste acima
- [ ] Implementação: `aluno/valor-devido.tsx` — mesma migração
- [ ] Implementação: remove `frontend/src/components/molecules/SeletorDePeriodo.tsx` se, após a migração das duas telas, não houver mais nenhum import dele no repo (`grep -rn "SeletorDePeriodo" frontend/src` deve retornar só o próprio arquivo antes de apagar — confirmar zero código morto)

### Inconsistências encontradas

_(Nenhuma até o momento — preencher durante a implementação se houver. Se o comportamento reativo (sem botão "Consultar") do Professor não se replicar de forma direta pro Aluno por alguma diferença de hook/estado, é aceitável manter um botão "Consultar" explícito nas telas do Aluno — o critério de aceite do card exige o calendário, não necessariamente remover o botão; registrar a decisão aqui se isso acontecer.)_
