import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { CadastroConfirmado } from '@/components/molecules/CadastroConfirmado';
import { CadastroUsuarioForm } from '@/components/organisms/CadastroUsuarioForm';
import { Topbar } from '@/components/organisms/Topbar';
import { cadastrarAluno, verificarContatoAluno } from '@/lib/api/alunos';
import { useCadastroUsuario } from '@/lib/useCadastroUsuario';
import { MaxContentWidth } from '@/theme/tokens';

/**
 * Tela de cadastro independente de Aluno (issue #61): cria (ou reaproveita,
 * ver RN da issue) uma identidade de usuário com o papel Aluno, sem
 * nenhum vínculo com um Professor ainda — o vínculo nasce depois, no fluxo
 * de código de convite. Após sucesso, mostra uma confirmação inline — não
 * há área logada ainda para navegar (issue #18).
 */
export default function CadastroAlunoScreen() {
  const {
    nome,
    contato,
    erro,
    enviando,
    concluido,
    nomeReadonly,
    setNome,
    handleChangeContato,
    handleBlurContato,
    handleSubmit,
  } = useCadastroUsuario({ cadastrar: cadastrarAluno, verificarContato: verificarContatoAluno });

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <Topbar titulo="Cadastro de Aluno" />
      <View
        className="w-full flex-1 items-center justify-center self-center px-four"
        style={{ maxWidth: MaxContentWidth }}
      >
        {concluido ? (
          <CadastroConfirmado papel="Aluno" />
        ) : (
          <CadastroUsuarioForm
            nome={nome}
            contato={contato}
            erro={erro}
            enviando={enviando}
            nomeReadonly={nomeReadonly}
            onChangeNome={setNome}
            onChangeContato={handleChangeContato}
            onBlurContato={handleBlurContato}
            onSubmit={handleSubmit}
          />
        )}
      </View>
    </SafeAreaView>
  );
}
