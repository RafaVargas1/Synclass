# Requisitos de segurança

Regras concretas, não uma lista genérica de OWASP — cada item aqui já tem
um padrão real no código do Synclass, citado como referência.

## Validação de entrada

- Todo DTO recebido em um controller da `Api` é validado antes de chegar
  no `Domain` — mensagens de exceção incluem o valor problemático e o
  formato esperado (ver `code-style.md#estilo-de-código`), nunca um erro
  genérico "entrada inválida".
- Normalize contato (e-mail/telefone: espaços, maiúsculas) antes de
  comparar duplicidade ou buscar por ele — edge point já registrado em
  mais de uma issue de cadastro (ex: issue #1, #61).

## Autorização (checagem de posse)

Todo recurso que pertence a um Professor específico precisa confirmar
posse antes de qualquer leitura/escrita — nunca confie só no `id` do
recurso vindo da rota/request. Padrão de referência:
`HorarioService.BuscarDoProfessorAsync` (`backend/src/Synclass.Domain/Horarios/HorarioService.cs`),
`internal` (não `private`) especificamente para ser reaproveitado por
outros services que precisam da mesma checagem (ex:
`AlocacaoHorarioService`, issue #8) em vez de reimplementá-la. Ao
adicionar um novo endpoint que opera sobre um recurso de Professor ou
Aluno, reaproveite o método de busca-com-checagem-de-posse já existente
na camada de domínio correspondente — não adicione uma checagem paralela
na `Api`.

## Autenticação

- Login é por código de uso único (OTP) enviado ao contato de cadastro
  (issue #18) — o código nunca aparece em log de negócio
  (`CodigoOtpSolicitado`, o evento estruturado, nunca inclui o código em
  si); só o `NotificadorDeLog` (implementação de desenvolvimento) loga o
  valor, num evento propositalmente separado (`OtpEnviadoParaDesenvolvimento`).
  Ao trocar `NotificadorDeLog` por uma integração real, garanta que o
  código continue fora de qualquer log de produção.
- Convites (`Convite.cs`) usam token de alta entropia (32 bytes aleatórios
  criptográficos, base64url) para o link — nunca reduza a entropia do
  token pensando em "ficar mais legível"; legibilidade é o papel do código
  curto de 5 dígitos (issue #62), que é um recurso à parte com sua própria
  regra de unicidade (ver `business-rules.md#convites`), não um substituto
  do token.

## Segredos

- Chave de API (ex: `DEEPSEEK_API_KEY`) vive em `.env` (nunca commitado —
  só `.env.example` com o nome da variável vazio) ou em variável de
  ambiente já exportada. **Nunca** aceite uma chave como argumento de CLI
  — vaza em `ps`/histórico do shell. Ver `scripts/deepseek-call.sh` como
  referência do padrão correto (lê de `.env`/ambiente, nunca de `$1`).
  `scripts/deepseek-agent.mjs` segue a mesma convenção.
- Nenhum segredo em log estruturado, mesmo em nível debug.

## Dados sensíveis e PII em log

Log estruturado (JSON + `TrackId`, ver `architecture.md#logs-estruturados-e-track-id`)
não deve incluir: senha/código OTP, token de convite completo (logue um
prefixo/hash se precisar correlacionar, não o valor usável), payload bruto
de requisição quando ele carrega dado sensível (contato, nome — logue o
`id` do recurso, não o payload).

## Débitos conhecidos

Registrados aqui para não ficarem invisíveis, sem implementação prevista
neste momento (mudança de CI é escopo separado, fora do pedido que criou
este documento):

- **Sem scanner de dependência/vulnerabilidade no CI** — nenhum
  `dependabot.yml`, nenhum `npm audit`/`dotnet list package --vulnerable`
  automatizado, nenhum CodeQL. `.github/workflows/` hoje só roda
  format/build/test (backend) e lint/typecheck/test (frontend).
- **Sem cobertura mínima de teste configurada** (ver
  `testing-standards.md#cobertura-por-camada`).
- **`scripts/deepseek-agent.mjs` roda `run_command` sem sandbox completo**
  — só um guard mínimo contra padrões obviamente destrutivos (`rm -rf /`,
  `git push --force`, `sudo`) e log de todo comando executado. Aceito como
  piso razoável dado que o resto do pipeline autônomo já roda com
  `--dangerously-skip-permissions`; reforçar isso é trabalho futuro, não
  bloqueia o uso atual do harness.
