# Guia de estilo de código

Este documento é o **orientador central** de todo o código do projeto Synclass —
backend (.NET/C#) e frontend (React Native/TypeScript). Qualquer decisão de estilo
não coberta aqui deve seguir a convenção padrão da linguagem/framework em uso e,
na dúvida, favorecer clareza sobre concisão.

## Estilo de código

- Funções: 4 a 20 linhas. Se passar disso, divida.
- Arquivos: até 500 linhas. Divida por responsabilidade.
- Uma coisa por função, uma responsabilidade por módulo (SRP).
- Nomes: específicos e únicos. Evite `data`, `handler`, `Manager`.
  Prefira nomes que retornem menos de 5 ocorrências ao dar grep no código.
- Tipos: explícitos. Nada de `any` (TypeScript), `dynamic`/`object` genérico (C#)
  ou funções sem tipo de retorno declarado.
- Sem duplicação de código. Extraia lógica compartilhada para uma função/módulo.
- Prefira retornos antecipados (*early return*) a ifs aninhados. Máximo de 2 níveis
  de indentação.
- Mensagens de exceção devem incluir o valor problemático e o formato esperado
  (ex: `throw new ArgumentException($"Duração inválida: {duracao}. Esperado um valor entre 15 e 240 minutos.")`).

## Comentários

- Mantenha comentários já existentes. Não os remova em um refactor — eles carregam
  intenção e proveniência.
- Escreva o PORQUÊ, não o QUÊ. Não escreva `// incrementa contador` acima de `i++`.
- Docstrings/XML doc em funções e métodos públicos: intenção + um exemplo de uso.
- Referencie números de issue / hash de commit quando uma linha existir por causa
  de um bug específico ou de uma restrição externa.

## Testes

Ver [`testing-standards.md`](testing-standards.md) — princípios F.I.R.S.T,
ordem do TDD, regra de teste escopado durante o loop vs. suíte completa
antes do PR, cobertura por camada e mocks. Comandos rápidos:
Backend `dotnet test` (a partir de `backend/`), Frontend `npm test` (a
partir de `frontend/`).

## Dependências

- Injete dependências via construtor/parâmetro, nunca via globais/import direto
  de instância concreta.
- Envolva bibliotecas de terceiros com uma interface fina de propriedade do
  projeto (ex: `IWhatsAppNotifier` envolvendo o SDK de terceiros escolhido,
  `IClock` envolvendo `DateTime.UtcNow`/`Date.now()` para permitir teste).

## Estrutura

- Siga a convenção do framework (ASP.NET Core no backend, Expo Router no
  frontend).
- Prefira módulos pequenos e focados a arquivos "deus" (*god files*).
- Caminhos previsíveis:
  - Backend: `Controllers/`, `Domain/`, `Infrastructure/`, `Tests/`.
  - Frontend: `src/components/{atoms,molecules,organisms,templates}`,
    `src/screens`, `src/lib`, `__tests__`.

## Formatação

- Use o formatador padrão da linguagem e não discuta estilo além disso:
  - C#: `dotnet format`.
  - TypeScript/React Native: `prettier` (via `npm run format`).

## Logging

- JSON estruturado para logs de debug/observabilidade, sempre incluindo o
  *track id* (ver [`architecture.md`](architecture.md#logs-estruturados-e-track-id)).
- Texto plano apenas para saída de CLI voltada ao usuário (ex: scripts de
  setup, mensagens de terminal).
