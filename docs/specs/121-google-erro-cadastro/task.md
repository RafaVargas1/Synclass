# Task: aluno/index.tsx e professor/cadastro.tsx sem try/catch no login Google (#121)

Card: https://github.com/RafaVargas1/Synclass/issues/121

Migração mecânica: reaproveitar `useAutenticadoGoogle` (extraído no PR
#118) nas duas telas restantes que ainda chamavam `definirSessao`
diretamente sem tratamento de falha.

## Ordem de execução

- [x] Teste de componente: `professor/cadastro.tsx` — mostra erro inline e não navega quando `definirSessao` falha
- [x] Implementação: `professor/cadastro.tsx` usa `useAutenticadoGoogle` em vez de `definirSessao`/`router.replace` direto
- [x] Teste de componente: `aluno/index.tsx` — mesmo cenário
- [x] Implementação: `aluno/index.tsx` mesma migração

### Inconsistências encontradas

_(Nenhuma — migração direta do mesmo padrão já estabelecido no PR #118.)_
