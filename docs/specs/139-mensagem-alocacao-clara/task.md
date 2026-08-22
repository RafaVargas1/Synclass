# Task: mensagem clara na tela de Alocação de Alunos (#139, fecha #126)

Card: https://github.com/RafaVargas1/Synclass/issues/139

Leia `implementation.md` ANTES do primeiro item — ele já resolve a
estrutura de estados, o texto exato de cada mensagem, e onde buscar
horários. Implemente exatamente isso.

## Ordem de execução

- [x] Teste (`alocacoes.test.tsx`): cenário "sem horários cadastrados"
      (`listarHorarios` mockado retornando `[]`) → mostra a mensagem
      "Nenhum horário cadastrado ainda." + link "Cadastrar horários", sem
      nada sobre modelo. Ver falhar.
- [x] Implementação mínima: função `resolverEstadoAlocacao` +
      `MensagemDoEstado` de `implementation.md`, e o ajuste no fetch pra
      buscar horários antes da decisão de estado (ver seção "Desenho
      novo").
- [x] Teste: cenário "modelo não configurado, horários existem" →
      "Configure o modelo de agendamento antes de alocar Alunos."
- [x] Teste: cenário "modelo Livre, ninguém inscrito" → mensagem
      explicando o modelo Livre + "ninguém se inscreveu ainda", SEM a
      palavra "Vago" em nenhum lugar do texto renderizado.
- [x] Teste: cenário "modelo Livre, com alguma alocação" → mensagem
      explicando o modelo Livre, sem a frase de "ninguém se inscreveu".
- [x] Teste: cenário "modelo Fixo/Híbrido, horários existem" → comporta-se
      como hoje (`AlocacoesConteudo` renderiza normalmente, sem
      `ErrorMessage`) — não regredir.
- [x] `grep -in "vago" frontend/src/app/professor/\[professorId\]/alocacoes.tsx
      frontend/src/app/professor/\[professorId\]/alocacoes.test.tsx` —
      confirme que não sobrou nenhuma ocorrência (nem no código nem nos
      testes) antes de marcar concluído.
- [x] Refatore se necessário: releia o diff final contra
      `docs/spec/code-style.md` e `docs/spec/engenharia-de-qualidade.md`.

## Inconsistências encontradas

### Item 7 (grep por "vago") contradiz `implementation.md` e os próprios critérios de aceite dos itens 4 e 5

O item 7 pede literalmente que **não sobre nenhuma ocorrência** de "vago"
(nem no código nem nos testes). Mas isso é insatisfazível junto com o
restante da Task:

1. `implementation.md` (fonte da decisão de design, seção "Desenho novo")
   exige explicitamente `if (carregamento.modeloAgendamento ===
   ModeloAgendamento.Vago)` em `resolverEstadoAlocacao` — o *nome do enum*
   do backend (que a própria frase do item 4 reconhece ser interno, nunca
   mostrado ao usuário) contém a substring "Vago". Removê-lo violaria o
   desenho mandatório.
2. Os testes dos itens 4 e 5 mockam o modelo Livre como
   `modeloAgendamento: ModeloAgendamento.Vago` (mesma necessidade do
   enum), e o item 4 ainda exige *verificar a ausência* de "Vago" no texto
   renderizado (`expect(screen.queryByText(/vago/i)).toBeNull()`), cuja
   própria consulta contém a substring "vago".

Ou seja: o critério de aceite do item 7, no literal, colide com o item 4
(que manda mockar `ModeloAgendamento.Vago` e assertar a ausência, ambas as
coisas precisam da string) e com `implementation.md` (que manda comparar
com `ModeloAgendamento.Vago`).

**Leitura razoável adotada (a menos que se decida o contrário):** a
intenção do item 7 é garantir que **nenhum texto visível ao usuário**
contenha "Vago" — o problema original era a const
`MensagemModeloVagoTexto` ('O modelo de agendamento Vago...'), já removida
nesta Task. As referências a `ModeloAgendamento.Vago` (lógica + mocks) são
internas e necessárias, e o novo assertion `/vago/i` garante a ausência no
texto renderizado. Se o grep literal zero for obrigatório, é preciso
decidir (a) substituir a comparação por `=== 0` e os mocks por `0` em vez
do enum (enfraquecendo a legibilidade contra `implementation.md`), e (b)
abrir mão do assertion de ausência do item 4.
