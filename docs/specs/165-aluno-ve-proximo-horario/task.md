# Task: Aluno vê próximo horário marcado direto no Painel (#165)

Card: https://github.com/RafaVargas1/Synclass/issues/165

Leia `implementation.md` ANTES do primeiro item — a ambiguidade
arquitetural encontrada na investigação (ver "Inconsistências
encontradas" abaixo) já foi **resolvida**: alternativa A escolhida
(endpoint novo `GET /alunos/professores`). Não reabra essa decisão nem
re-investigue as alternativas B/C — implemente o desenho já pronto.

## Ordem de execução

- [x] Teste (Api, `VinculosAlunoEndpointTests.cs`, criar): Aluno com
      matrícula em dois Professores → lista os dois; Aluno sem matrícula
      → lista vazia (200, não 404). Ver falhar.
- [x] Implementação mínima: cria
      `backend/src/Synclass.Api/Controllers/VinculosAlunoController.cs`
      (`GET /alunos/professores`, código exato em `implementation.md`).
- [x] Teste (`frontend/src/lib/api/vinculosAluno.test.ts`, criar):
      sucesso, lista vazia, erro de rede.
- [x] Implementação mínima: cria `frontend/src/lib/api/vinculosAluno.ts`
      (código exato em `implementation.md`).
- [x] Teste (`ResumoProximoHorario.test.tsx`, criar): com dado preenchido
      mostra Professor+data+hora formatados; com `null` mostra "Nenhum
      horário marcado no momento."
- [x] Implementação mínima: cria
      `frontend/src/components/organisms/ResumoProximoHorario.tsx`
      (código exato em `implementation.md` — siga o padrão visual de
      `ResumoValorReceber.tsx`, issue #166).
- [x] Teste (`frontend/src/app/painel/index.test.tsx`): estende a suíte
      existente com os 4 cenários descritos em `implementation.md`
      (um Professor; dois Professores, mostra o mais próximo; sem
      vínculo/sem aula, estado vazio; papel Professor não chama as
      funções novas).
- [x] Implementação mínima: `frontend/src/app/painel/index.tsx` — busca
      vínculos + agrega próximas aulas quando `papelAtivo === 'Aluno'`,
      renderiza `ResumoProximoHorario` (lógica em `implementation.md`).
- [ ] `npm run lint && npm run typecheck && npm test` (frontend) e
      `dotnet format --verify-no-changes && dotnet test` (backend) —
      suíte completa de cada lado, verde.
- [ ] Verificação visual (Playwright, se disponível): `/painel` como
      Aluno com sessão fake, confirme o card de resumo aparece com dado
      mockado, sem quebrar o restante da tela. Se não for possível
      verificar no ambiente, registre isso em "## Inconsistências
      encontradas" em vez de pular em silêncio.
- [ ] Refatore se necessário: releia o diff final contra
      `docs/spec/code-style.md`.

## Inconsistências encontradas (resolvida — mantida como registro)

A RN do card mandava "sem endpoint novo, a menos que a investigação
confirme que não há forma de listar horários futuros por todos os
Professores de uma vez, agregando via vínculos de `usePerfilLogado`". A
investigação confirmou que não há forma hoje, E que a premissa do
fallback é falsa neste repositório (`usePerfilLogado`/`GET /usuarios/me`
nunca carregaram vínculos, não existe tela "Meus Professores" do lado
Aluno). **Decisão tomada**: alternativa A — criar `GET
/alunos/professores`, exatamente a exceção que a própria RN autoriza
quando a condição se verifica. Alternativas B (proxy via
historico-frequencia, semanticamente errado) e C (cortar o escopo,
contradiz a história de usuário) foram descartadas. Ver
`implementation.md` para o desenho completo.

## Fora de escopo

Ver seção "Fora de escopo" de `implementation.md`.
