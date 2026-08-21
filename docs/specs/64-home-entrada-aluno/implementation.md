# Implementação: Home apresenta com clareza os caminhos de entrada (#64)

Composição de UI sobre o que as Tasks #61 (`CadastroUsuarioService`/tela de
cadastro independente de Aluno) e #63 (aceite de convite por código de 5
dígitos) já publicaram — nenhum endpoint novo, nenhuma migration.

## Entidades/classes afetadas

Só frontend:

- `components/organisms/HomeHero.tsx` — dois CTAs (`onEntrarComoProfessor`,
  `onEntrarComoAluno`) no lugar do botão único + link ambíguo + card
  estático de Aluno (`PainelDoAluno`, removido).
- `components/organisms/HomeHero.test.tsx` — reescreve as duas primeiras
  asserções para os rótulos/props novos; mantém a de `onLogin`.
- `components/templates/HomeTemplate.tsx` (+ teste) — propaga
  `onEntrarComoAluno`.
- `app/index.tsx` (+ teste, se existir) — `onEntrarComoAluno: () =>
  router.push('/aluno')`.
- `app/aluno/index.tsx` (novo) — tela única do Aluno, ver Desenho abaixo.
- `app/aluno/index.test.tsx` (novo).
- Removidos: `app/aluno/entrar-turma.tsx`, `entrar-turma.test.tsx`,
  `app/aluno/cadastro.tsx`, `cadastro.test.tsx` — o conteúdo funcional dos
  dois vira `app/aluno/index.tsx`.

Nenhuma mudança em `lib/api/alunos.ts`, `lib/api/convites.ts`,
`lib/normalizarCodigoConvite.ts`, `lib/useCadastroUsuario.ts`,
`components/organisms/CadastroUsuarioForm.tsx`, ou em qualquer camada de
backend — são reaproveitados tal como estão.

## Contrato

Nenhum contrato de API novo. A tela nova consome os dois clientes já
existentes:

- `cadastrarAluno({ nome, contato })` — `lib/api/alunos.ts` (issue #61).
- `aceitarConvitePorCodigo({ codigo, nome, contato })` — `lib/api/convites.ts`
  (issue #63).
- `verificarContatoAluno(contato)` — mesmo comportamento de
  `app/aluno/cadastro.tsx` hoje (readonly do campo Nome quando o contato já
  tem identidade), reaproveitado nos dois modos da tela nova.

Rota nova: `/aluno` (`app/aluno/index.tsx`, mesmo padrão de `app/login/index.tsx`
para `/login`). Rotas removidas: `/aluno/entrar-turma`, `/aluno/cadastro`
(nenhuma outra tela linkava para elas — ver Dependência de outras Tasks).

## Desenho

Uma única tela, sem alternância de rota nem de "aba" escondendo um dos dois
formulários — os dois ficam visíveis ao mesmo tempo, porque o campo de
código é **opcional**:

```
FormField "Código da turma (opcional)"   <- normalizarCodigoConvite, maxLength 5
Paragraph explicando o vínculo posterior <- sempre visível, perto do campo acima
CadastroUsuarioForm (nome, contato)      <- reaproveitado tal como está
```

No `onSubmit` do `CadastroUsuarioForm`, decide qual cliente chamar
conforme o código (já normalizado) estar vazio ou não:

```ts
async function handleSubmit() {
  setEnviando(true);
  setErro(undefined);

  const resultado = codigo
    ? await aceitarConvitePorCodigo({ codigo, nome, contato })
    : await cadastrarAluno({ nome, contato });

  setEnviando(false);
  if (!resultado.sucesso) {
    if (codigo && ehConviteExpirado(resultado.mensagem)) {
      setExpirado(true);
      return;
    }
    setErro(resultado.mensagem);
    return;
  }
  setUsouCodigo(Boolean(codigo));
  setNomeConfirmado(resultado.nome);
}
```

`ehConviteExpirado` é a mesma função hoje privada em `entrar-turma.tsx`
(procura `"expirou"` na mensagem) — só faz sentido quando o envio usou
código, por isso a guarda `codigo &&`.

Estado de confirmação, condicionado a qual caminho foi usado (mesmo
`usuarioId`/`nome` não muda o texto mostrado ao Aluno, mas o vínculo com o
Professor sim):

```tsx
{expirado ? (
  <ConviteExpirado />
) : nomeConfirmado ? (
  usouCodigo ? (
    <AceiteConviteConfirmado nome={nomeConfirmado} />
  ) : (
    <CadastroConfirmado papel="Aluno" />
  )
) : (
  <FormularioEntradaAluno ... />
)}
```

O texto fixo de explicação (critério de aceite 3 da issue #64) fica como um
`Paragraph` entre o campo de código e o `CadastroUsuarioForm`, sempre
visível (não é uma dica que aparece só quando o campo está vazio — é
contexto permanente da tela): algo como *"Tem o código de 5 dígitos que seu
Professor te passou? Preencha acima. Sem código em mãos, só cadastre seu
nome e contato abaixo — o vínculo com o Professor é feito depois, por
código ou link."*

`onBlurContato` chama `verificarContatoAluno` nos dois modos (o card #61 já
cobre esse comportamento para identidade existente; aplicá-lo também ao
caminho por código é consistente e não é uma regra nova — só estende o
reaproveitamento do componente, sem introduzir lógica de negócio adicional).

Estado local do componente (todo em `app/aluno/index.tsx`, sem hook
compartilhado novo — `useCadastroUsuario` não cobre o ramo por código, então
não é reaproveitado aqui; replicar seu shape inline é mais simples que
generalizá-lo para os dois clientes com contratos de retorno diferentes):
`codigo`, `nome`, `contato`, `erro`, `enviando`, `nomeReadonly`,
`nomeConfirmado`, `usouCodigo`, `expirado`.

## Modelo de dados

Nenhum.

## Edge points

- Código vazio no submit → cadastro independente, não uma tentativa de
  aceite de convite com string vazia (guarda `codigo ? ... : ...`, não deixa
  a Api decidir isso).
- Normalização do código idêntica à de `entrar-turma.tsx`
  (`normalizarCodigoConvite`, só dígitos, tolera máscara).
- `ConviteExpirado` só é alcançável pelo caminho com código — cadastro
  independente não tem conceito de expiração.
- `HomeHero`/`HomeTemplate` continuam sem saber nada sobre o conteúdo da
  tela do Aluno — só navegam para `/aluno` (Critério técnico explícito da
  issue #64: `HomeHero.tsx` continua só o ponto de entrada).

## Dependência de outras Tasks

Depende das issues #61 e #63 (ambas já mergeadas em `main`:
`CadastroUsuarioService`/`app/aluno/cadastro.tsx` e
`ConviteService.AceitarPorCodigoAsync`/`app/aluno/entrar-turma.tsx`). Nenhum
bloqueio — a Task consome só o que essas duas já publicaram no frontend
(nenhum dos dois arquivos de tela tinha entrada de navegação própria a
partir da Home antes desta Task, confirmado em
`docs/specs/61-cadastro-aluno/implementation.md#edge-points` e
`docs/specs/63-entrar-turma-codigo/implementation.md#dependência-de-outras-tasks`
— por isso remover as duas rotas antigas em favor da tela única não quebra
nenhum link existente). Não depende da issue #77 (menu de navegação
persistente, ainda não implementada) — a rota `/aluno` fica estável o
suficiente para ser referenciada por aquele card no futuro.
