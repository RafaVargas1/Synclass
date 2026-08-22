# Implementação: Aluno autenticado entra em nova turma por código (#144)

## Ambiguidades da issue — já resolvidas (não é pra DeepSeek reabrir)

A issue original listava duas perguntas pendentes de investigação antes de
especificar. Ambas foram respondidas lendo o código atual:

1. **"O backend já suporta um Aluno com múltiplas matrículas?"** — Sim,
   por desenho, sem exigir nenhuma mudança. Ver
   `backend/src/Synclass.Domain/Matriculas/Matricula.cs` (doc do topo da
   classe): não há índice único em `AlunoUsuarioId` isolado, só em
   `(ProfessorId, IdentificadorProvisorio)` — um mesmo Aluno pode ter uma
   `Matricula` independente por Professor, já formalizado em teste (issue
   #5). `ConviteService.VincularMatriculaAsync`
   (`backend/src/Synclass.Domain/Convites/ConviteService.cs`) já lida com
   "Aluno já existe, Professor é novo": quando não há matrícula de origem
   nem vínculo já existente com aquele Professor, cria
   `Matricula.CriarVinculada(convite.ProfessorId, alunoUsuarioId, _clock)`
   — uma matrícula nova, plena, pro Aluno já identificado. Isso é
   exatamente o caminho que este card precisa, e já está coberto por
   `ConviteAceitoEndpointTests`/testes de Domain existentes — não toque
   nessa lógica.
2. **"Precisa pedir nome/contato de novo?"** — Não, mas não por mudança
   de contrato do endpoint de aceite (`POST /convites/codigo/{codigo}/aceite`
   continua exigindo `nome`+`contato` no corpo — é assim que
   `ConviteService.AceitarResolvidoAsync` casa "quem está aceitando" com
   o `Usuario` já existente, via `ObterOuCriarUsuarioAsync` buscando por
   contato). O que muda é só o FRONTEND: em vez de pedir pro Aluno digitar
   de novo, o app já sabe o nome/contato de quem está logado (`GET
   /usuarios/me`) e os envia por conta própria — o Aluno só digita o
   código. Isso também fecha o loop de segurança de graça: se o Aluno
   autenticado tentar usar um código que não é dele, o backend já rejeita
   com `ConviteContatoDivergenteException` (contato da sessão ≠ contato do
   convite), sem precisar de nenhuma lógica nova de validação.

**Conclusão de escopo**: card fullstack pequeno, não epic. Backend só
precisa expor `Contato` em `GET /usuarios/me` (não expõe hoje). Todo o
resto (nova matrícula, N:N Aluno-Professor, validação de contato) já
existe e não muda.

## Backend

### `backend/src/Synclass.Api/Controllers/UsuariosController.cs`

`UsuarioPerfilResponse` (linha 81) não carrega `Contato` hoje — precisa,
pro frontend pré-preencher o aceite de convite sem pedir de novo ao Aluno.

**Antes**:
```csharp
public sealed record UsuarioPerfilResponse(Guid UsuarioId, string Nome);
```

**Depois**:
```csharp
public sealed record UsuarioPerfilResponse(Guid UsuarioId, string Nome, string Contato);
```

Os dois call sites que constroem esse record (método `Me`, linha ~43, e
`AtualizarNome`, linha ~56) passam a incluir `usuario.Contato` (propriedade
já existente em `Usuario`, `backend/src/Synclass.Domain/Usuarios/Usuario.cs:28`):

```csharp
// Me:
return Ok(new UsuarioPerfilResponse(usuario.Id, usuario.Nome, usuario.Contato));

// AtualizarNome:
return Ok(new UsuarioPerfilResponse(usuario.Id, usuario.Nome, usuario.Contato));
```

Mudança aditiva e retrocompatível (novo campo no JSON de resposta, nenhum
campo removido/renomeado) — não é breaking change de contrato.

### Teste: `backend/tests/Synclass.Api.Tests/UsuariosControllerTests.cs`

Leia o arquivo primeiro pra seguir o padrão de setup já usado (autenticação
via `AutenticacaoTestHelper`, mesmo padrão de outros arquivos desta pasta).
Adicione (ou estenda um teste existente do `GET /usuarios/me`) uma
asserção de que a resposta inclui `Contato` igual ao contato usado no
cadastro do usuário de teste.

## Frontend

### `frontend/src/lib/api/usuarios.ts`

**Antes**:
```ts
export type BuscarPerfilResultado =
  | { sucesso: true; usuarioId: string; nome: string }
  | { sucesso: false; mensagem: string };
```
```ts
return { sucesso: true, usuarioId: corpo?.usuarioId ?? '', nome: corpo?.nome ?? '' };
```

**Depois**:
```ts
export type BuscarPerfilResultado =
  | { sucesso: true; usuarioId: string; nome: string; contato: string }
  | { sucesso: false; mensagem: string };
```
```ts
return { sucesso: true, usuarioId: corpo?.usuarioId ?? '', nome: corpo?.nome ?? '', contato: corpo?.contato ?? '' };
```

### `frontend/src/lib/usePerfilLogado.ts`

Expõe `contato` do mesmo jeito que já expõe `nome` — mesmo hook, sem criar
um segundo. **Antes**: `EstadoPerfilLogado` tem `usuarioId`, `nome`, `erro`,
`tentarNovamente`. **Depois**: adiciona `contato: string | undefined`,
seguindo exatamente o padrão já usado por `nome` (`useState`, setado no
`.then(resultado => ...)`, devolvido no objeto final). Não mude a lógica de
"para de buscar assim que resolve uma vez" — só adiciona o campo novo ao
mesmo fluxo.

### `frontend/src/components/organisms/EntrarEmNovaTurmaForm.tsx` (novo)

Só pede o código — nome/contato já vêm da sessão, não são campos do
formulário. Segue o padrão de `VerificarCodigoForm.tsx`
(`frontend/src/components/organisms/VerificarCodigoForm.tsx`) pro campo
mascarado de código, sem o botão de "Reenviar" (não existe reenvio nesse
fluxo — é o código de convite do Professor, não um OTP), e com 5 dígitos
(não 6, ver `ConviteService` — código curto de convite é de 5 dígitos,
diferente do OTP de login):

```tsx
import { View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { FormField } from '@/components/molecules/FormField';

export type EntrarEmNovaTurmaFormProps = {
  codigo: string;
  erro?: string;
  enviando: boolean;
  onChangeCodigo: (codigo: string) => void;
  onSubmit: () => void;
};

/**
 * Organismo: formulário de entrada em nova turma via código de convite
 * (issue #144) — só pede o código; nome/contato do Aluno já autenticado
 * são enviados por trás das cenas (ver `entrar-em-turma.tsx`), sem exibir
 * campo pra eles.
 */
export function EntrarEmNovaTurmaForm({
  codigo,
  erro,
  enviando,
  onChangeCodigo,
  onSubmit,
}: EntrarEmNovaTurmaFormProps) {
  return (
    <View className="w-full gap-four">
      <FormField
        label="Código da turma"
        value={codigo}
        onChangeText={onChangeCodigo}
        placeholder="00000"
        keyboardType="number-pad"
        maxLength={5}
      />
      {erro ? <ErrorMessage>{erro}</ErrorMessage> : null}
      <Button
        label={enviando ? 'Entrando...' : 'Entrar na turma'}
        onPress={onSubmit}
        disabled={enviando}
      />
    </View>
  );
}
```

### `frontend/src/app/aluno/entrar-em-turma.tsx` (novo)

Segue o padrão de tela autenticada já usado em todo o app (`TopbarAutenticada`
+ `SafeAreaView` + `MaxContentWidth`, ver `frontend/src/app/aluno/historico-frequencia.tsx`
como referência direta de estrutura). Usa `useSessao()` pro `token` e
`usePerfilLogado(token)` pro nome/contato do Aluno logado — mesmo padrão
de `MenuNavegacao.tsx`. Chama `aceitarConvitePorCodigo` (já existe em
`frontend/src/lib/api/convites.ts`, issue #63) — nenhuma função de Api
nova.

```tsx
import { useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Heading } from '@/components/atoms/Heading';
import { Paragraph } from '@/components/atoms/Paragraph';
import { EntrarEmNovaTurmaForm } from '@/components/organisms/EntrarEmNovaTurmaForm';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import { aceitarConvitePorCodigo } from '@/lib/api/convites';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { usePerfilLogado } from '@/lib/usePerfilLogado';
import { MaxContentWidth } from '@/theme/tokens';

/**
 * Tela autenticada de entrada em nova turma por código de convite (issue
 * #144) — nome/contato do Aluno já logado (`usePerfilLogado`) são
 * enviados junto do código, sem pedir de novo (a Api casa por contato e
 * cria uma nova Matricula vinculada ao Professor do convite, ver
 * implementation.md desta issue). Diferente de `app/convite/[token].tsx`
 * (issue #2, fluxo público/não-autenticado com CadastroUsuarioForm) — este
 * fluxo é só pro Aluno que já tem conta.
 */
export default function EntrarEmNovaTurmaScreen() {
  const { token } = useSessao();
  const perfil = usePerfilLogado(token);
  const [codigo, setCodigo] = useState('');
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [enviando, setEnviando] = useState(false);
  const [concluido, setConcluido] = useState(false);

  async function handleSubmit() {
    if (!perfil.nome || !perfil.contato) {
      setErro('Não foi possível confirmar seu perfil. Tente novamente em instantes.');
      return;
    }
    setEnviando(true);
    setErro(undefined);

    const resultado = await aceitarConvitePorCodigo({ codigo, nome: perfil.nome, contato: perfil.contato });

    setEnviando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setConcluido(true);
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <TopbarAutenticada titulo="Entrar em nova turma" />
      <View
        className="w-full flex-1 items-center justify-center self-center px-four"
        style={{ maxWidth: MaxContentWidth }}
      >
        {concluido ? (
          <View accessibilityRole="alert" className="items-center gap-two">
            <Heading level={1}>Turma adicionada!</Heading>
            <Paragraph>Você já pode ver os horários deste Professor nas suas telas de Aluno.</Paragraph>
          </View>
        ) : (
          <EntrarEmNovaTurmaForm
            codigo={codigo}
            erro={erro}
            enviando={enviando}
            onChangeCodigo={setCodigo}
            onSubmit={handleSubmit}
          />
        )}
      </View>
    </SafeAreaView>
  );
}
```

Não crie uma molécula nova de confirmação por um bloco de 2 linhas
(`Heading`+`Paragraph`) usado uma única vez — siga o mesmo racional já
usado em outras telas de confirmação inline curtas desta base (ex:
`AlunoProvisorioConfirmado`, que já existe como molécula porque é
reaproveitado; aqui não há reaproveito, então fica inline na própria tela,
sem criar arquivo novo em `components/molecules/` só por simetria).

### `frontend/src/lib/secoesPorPapel.ts`

Adiciona a seção nova em `secoesAluno()`, entre as duas existentes:

**Antes**:
```ts
export function secoesAluno(): Secao[] {
  return [
    { label: 'Ver histórico de frequência', href: '/aluno/historico-frequencia' },
    { label: 'Ver valor devido', href: '/aluno/valor-devido' },
  ];
}
```

**Depois**:
```ts
export function secoesAluno(): Secao[] {
  return [
    { label: 'Entrar em nova turma', href: '/aluno/entrar-em-turma' },
    { label: 'Ver histórico de frequência', href: '/aluno/historico-frequencia' },
    { label: 'Ver valor devido', href: '/aluno/valor-devido' },
  ];
}
```

Fica primeiro na lista — "entrar em nova turma" é uma ação de aquisição de
vínculo novo, mais próxima do fluxo de "primeira vez" do que as duas
telas de consulta que já existiam (ver
`docs/spec/ux-heuristics.md#agrupamento-visual`: ordem por frequência/
papel de uso esperado, não ordem alfabética/de implementação).

## Testes

- `frontend/src/components/organisms/EntrarEmNovaTurmaForm.test.tsx`
  (novo): mesmo padrão de `VerificarCodigoForm.test.tsx` se existir (leia
  primeiro), senão o padrão já usado por outros arquivos de teste de
  formulário desta sessão (`CadastroAlunoProvisorioForm.test.tsx`) — cobre
  render do campo, `onChangeCodigo` chamado, `onSubmit` chamado, estado
  `enviando` desabilitando o botão.
- `frontend/src/app/aluno/entrar-em-turma.test.tsx` (novo): mocka
  `usePerfilLogado` (retornando nome/contato fixos), `aceitarConvitePorCodigo`
  e `TopbarAutenticada` (mesmo padrão de mock usado em
  `professor/alunos/cadastro.test.tsx`). Cenários: submit chama
  `aceitarConvitePorCodigo` com `{ codigo, nome, contato }` do perfil (sem
  pedir esses campos na tela — não deve haver nenhum `FormField` de
  nome/contato renderizado); sucesso mostra "Turma adicionada!"; erro da
  Api aparece inline sem travar a tela.
- `frontend/src/lib/usePerfilLogado.test.ts`: estende os testes existentes
  pra cobrir que `contato` também é resolvido a partir da resposta de
  `buscarPerfil`.
- `frontend/src/lib/api/usuarios.test.ts`: estende o teste de
  `buscarPerfil` pra cobrir o campo `contato` na resposta de sucesso.
- `frontend/src/lib/secoesPorPapel.test.ts`: estende o teste de
  `secoesAluno()` pra cobrir a nova seção "Entrar em nova turma" (rótulo e
  `href` exatos).
- Backend: ver seção "Teste" dentro de "Backend" acima.

## Edge points

- `perfil.nome`/`perfil.contato` ainda não resolvidos quando o Aluno tenta
  submeter antes do `GET /usuarios/me` responder: `handleSubmit` já trata
  isso (mensagem de erro pedindo pra tentar de novo, sem chamar a Api com
  dado vazio) — não precisa de spinner bloqueando o formulário inteiro,
  simetria com o resto do app onde o formulário fica interativo desde o
  primeiro render.
- Código de convite que não é do Aluno logado (contato divergente): já
  tratado pelo backend existente (`ConviteContatoDivergenteException` →
  400 com mensagem) — o `erro` inline já cobre esse caso via o mesmo
  caminho de qualquer outra rejeição de negócio, nenhum tratamento
  especial necessário.
- Código que já foi usado/expirado: mesma mensagem de erro genérica da Api
  (`ConviteExpiradoException`/`ConviteInvalidoException`) — diferente de
  `app/convite/[token].tsx`, que tem um estado dedicado `ConviteExpirado`;
  aqui não é necessário replicar esse estado dedicado (fora de escopo,
  Gherkin da issue não pede isso) — a mensagem inline já é suficiente.

## Fora de escopo

- Não mexer no fluxo público de `app/convite/[token].tsx` (issue #2) —
  continua existindo do jeito que está, pro visitante sem conta.
- Não adicionar um jeito de o Aluno ver TODOS os Professores vinculados
  numa lista própria — isso já é outra tela/necessidade (fora do escopo
  Gherkin desta issue, que só pede o aceite em si).
- Não mudar `ConviteService`/contrato de `POST /convites/codigo/{codigo}/aceite`
  — o único ponto tocado no backend é a resposta de `GET /usuarios/me`.
