# Task: Professor vê valor a receber do mês direto no Painel (#166)

Card: https://github.com/RafaVargas1/Synclass/issues/166 — sub-issue do épico
#127 (Painel como dashboard de verdade). Segunda das 3 Tasks.

## Ordem de execução

Não há migration nem endpoint novo (reaproveita `GET
/professores/{professorId}/valor-devido` sem query string — o backend usa o
mês corrente como default, ver `ValorDevidoController.TentarResolverPeriodo`).
Fase 3 é inteira no frontend.

- [x] Teste de componente (`frontend/src/app/painel/index.test.tsx`): cenário
  Gherkin 1 — Professor com Alunos com frequência no mês → vê o total somado
  (um Aluno com `valor: 300`, outro com `valor: 450` → "R$ 750,00") sem abrir
  "Ver valor devido".
- [x] Teste de componente (`frontend/src/app/painel/index.test.tsx`): cenário
  Gherkin 2 — Professor sem frequência no mês (lista vazia) → vê a mensagem
  "Nenhum valor a receber neste mês." e **não** vê um valor em destaque.
- [x] Teste de componente (`frontend/src/app/painel/index.test.tsx`): cenário
  Gherkin 2 (variante) — todos os Alunos com `semRegraDefinida: true` (valor
  `null`) → mesma mensagem de valor zerado.
- [x] Teste de componente (`frontend/src/app/painel/index.test.tsx`): cenário
  Gherkin 3 — o resumo soma exatamente os `valor` não nulos devolvidos pela
  mesma `listarValorDevido` (sem filtro de período) que a tela "Este mês"
  usa; assert que a chamada é feita **sem** query string `inicio`/`fim`
  (garante o mesmo contrato do filtro "Este mês", pois o backend então usa o
  mês corrente).
- [x] Teste de componente (`frontend/src/app/painel/index.test.tsx`): papel
  ativo Aluno **não** mostra a seção de "a receber" (resumo é só Professor).
- [x] Implementação mínima do cenário: criar organismo
  `frontend/src/components/organisms/ResumoValorReceber.tsx` (card de leitura
  puro, não-link) e montá-lo em `frontend/src/app/painel/index.tsx` entre a
  `Saudacao` e o bloco de `CardDeAcao`, só quando `papelAtivo === 'Professor'`
  e `usuarioId` resolvido.
- [x] Estado de conexão/erro do resumo (`ActivityIndicator` /
  `ErrorMessage`), seguindo o mesmo tratamento de `valor-devido.tsx`
  (`ConteudoDaConsulta`), sem duplicar: reutilizar `listarValorDevido` e
  tratar o `{ sucesso: false }` como erro identificável.
- [x] Log estruturado: nenhum endpoint novo nem evento novo — a consulta já
  loga `ConsultaValorDevidoRealizada` no `ValorDevidoController`; nada a
  adicionar nesta Task.

## Inconsistências encontradas

(nenhuma)
