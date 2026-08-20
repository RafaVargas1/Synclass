# Task: Home apresenta com clareza os caminhos de entrada de Professor e Aluno (#64)

Card: https://github.com/RafaVargas1/Synclass/issues/64

## Ordem de execução

- [ ] Teste de componente (`HomeHero.test.tsx`): dois CTAs distintos e
      rotulados "sou Professor" / "sou Aluno", cada um disparando o callback
      próprio (`onEntrarComoProfessor`, `onEntrarComoAluno`); nenhum texto
      ambíguo do tipo "Cadastrar como Professor" ou "Já tenho conta, entrar"
      cobrindo os dois papéis. Mantém a asserção de `onLogin` (link de login
      continua igualmente acessível).
- [ ] `HomeHero.tsx`: substitui o botão único + `PainelDoAluno` estático
      pelos dois CTAs (`Button label="sou Professor"` e
      `Button label="sou Aluno"`), preservando `onLogin` como está. Renomeia
      a prop `onGetStarted` para `onEntrarComoProfessor` e adiciona
      `onEntrarComoAluno`. Remove `PainelDoAluno` (não faz mais sentido como
      card estático — o caminho do Aluno agora é um CTA de mesmo peso).
- [ ] Propaga a prop nova por `HomeTemplate.tsx` (`onEntrarComoAluno`) e
      `app/index.tsx` (`router.push('/aluno')`), ajustando os testes
      existentes desses dois arquivos para o novo nome/prop.
- [ ] Teste de tela (`app/aluno/index.test.tsx`, novo arquivo): tela única
      mostra, simultaneamente, o campo de código de 5 dígitos e o formulário
      de nome/contato — sem navegação para trocar de "modo". Cobre os
      cenários abaixo (ver `implementation.md#desenho` para o comportamento
      exato):
  - Código de 5 dígitos preenchido + nome/contato → chama
    `aceitarConvitePorCodigo`, mostra `AceiteConviteConfirmado` no sucesso.
  - Código vazio + nome/contato → chama `cadastrarAluno` (cadastro
    independente), mostra `CadastroConfirmado` com `papel="Aluno"` no
    sucesso.
  - Código expirado → estado dedicado `ConviteExpirado` (mesmo
    comportamento herdado de `entrar-turma.tsx`).
  - Erro de negócio da Api (código inválido, contato já vinculado, contato
    inválido) → mensagem inline, sem crash, formulário continua editável.
  - Código mascarado (`"1-2-3-4-5"`) normalizado antes do envio (mesmo
    edge point de `entrar-turma.tsx`).
  - Texto explicando que, sem código, o vínculo com o Professor é feito
    depois (por código ou link) está sempre visível perto do campo de
    código — não é uma tela/rota separada.
- [ ] `app/aluno/index.tsx` (novo): implementa o desenho de
      `implementation.md#desenho`, reaproveitando `CadastroUsuarioForm`,
      `AceiteConviteConfirmado`, `CadastroConfirmado`, `ConviteExpirado`,
      `normalizarCodigoConvite`, `cadastrarAluno`, `verificarContatoAluno` e
      `aceitarConvitePorCodigo` já existentes — sem duplicar formulário nem
      lógica de normalização/validação já implementada nas Tasks #61/#63.
- [ ] Remove `app/aluno/entrar-turma.tsx` + `entrar-turma.test.tsx` e
      `app/aluno/cadastro.tsx` + `cadastro.test.tsx` — consolidados na tela
      única acima (nenhuma outra tela referenciava essas duas rotas, ver
      `implementation.md#dependência-de-outras-tasks`).
- [ ] Confere manualmente (leitura, não é item de TDD) que `/login`
      continua roteável a partir de `HomeHero`/`HomeTemplate` sem mudança de
      comportamento.
