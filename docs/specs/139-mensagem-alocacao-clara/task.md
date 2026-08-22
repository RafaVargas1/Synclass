# Task: mensagem clara na tela de Alocação de Alunos (#139, fecha #126)

Card: https://github.com/RafaVargas1/Synclass/issues/139

Leia `implementation.md` ANTES do primeiro item — ele já resolve a
estrutura de estados, o texto exato de cada mensagem, e onde buscar
horários. Implemente exatamente isso.

## Ordem de execução

- [ ] Teste (`alocacoes.test.tsx`): cenário "sem horários cadastrados"
      (`listarHorarios` mockado retornando `[]`) → mostra a mensagem
      "Nenhum horário cadastrado ainda." + link "Cadastrar horários", sem
      nada sobre modelo. Ver falhar.
- [ ] Implementação mínima: função `resolverEstadoAlocacao` +
      `MensagemDoEstado` de `implementation.md`, e o ajuste no fetch pra
      buscar horários antes da decisão de estado (ver seção "Desenho
      novo").
- [ ] Teste: cenário "modelo não configurado, horários existem" →
      "Configure o modelo de agendamento antes de alocar Alunos."
- [ ] Teste: cenário "modelo Livre, ninguém inscrito" → mensagem
      explicando o modelo Livre + "ninguém se inscreveu ainda", SEM a
      palavra "Vago" em nenhum lugar do texto renderizado.
- [ ] Teste: cenário "modelo Livre, com alguma alocação" → mensagem
      explicando o modelo Livre, sem a frase de "ninguém se inscreveu".
- [ ] Teste: cenário "modelo Fixo/Híbrido, horários existem" → comporta-se
      como hoje (`AlocacoesConteudo` renderiza normalmente, sem
      `ErrorMessage`) — não regredir.
- [ ] `grep -in "vago" frontend/src/app/professor/\[professorId\]/alocacoes.tsx
      frontend/src/app/professor/\[professorId\]/alocacoes.test.tsx` —
      confirme que não sobrou nenhuma ocorrência (nem no código nem nos
      testes) antes de marcar concluído.
- [ ] Refatore se necessário: releia o diff final contra
      `docs/spec/code-style.md` e `docs/spec/engenharia-de-qualidade.md`.

## Fora de escopo

Ver seção "Fora de escopo" de `implementation.md`.
