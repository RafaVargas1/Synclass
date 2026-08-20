import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { CadastroConfirmado } from '@/components/molecules/CadastroConfirmado';
import { CadastroUsuarioForm } from '@/components/organisms/CadastroUsuarioForm';
import { Topbar } from '@/components/organisms/Topbar';
import { cadastrarProfessor, verificarContatoProfessor } from '@/lib/api/professores';
import { useCadastroUsuario } from '@/lib/useCadastroUsuario';
import { MaxContentWidth } from '@/theme/tokens';

/**
 * Tela de cadastro de Professor (issue #1). Após sucesso, mostra uma
 * confirmação inline — não há área logada ainda para navegar (issue #18).
 */
export default function CadastroProfessorScreen() {
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
  } = useCadastroUsuario({ cadastrar: cadastrarProfessor, verificarContato: verificarContatoProfessor });

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <Topbar titulo="Cadastro de Professor" />
      <View
        className="w-full flex-1 items-center justify-center self-center px-four"
        style={{ maxWidth: MaxContentWidth }}
      >
        {concluido ? (
          <CadastroConfirmado papel="Professor" />
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
