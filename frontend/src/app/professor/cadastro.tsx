import { useLocalSearchParams } from 'expo-router';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { BotaoLoginGoogle } from '@/components/molecules/BotaoLoginGoogle';
import { BotaoLoginApple } from '@/components/molecules/BotaoLoginApple';
import { CadastroConfirmado } from '@/components/molecules/CadastroConfirmado';
import { CadastroUsuarioForm } from '@/components/organisms/CadastroUsuarioForm';
import { Topbar } from '@/components/organisms/Topbar';
import { cadastrarProfessor, verificarContatoProfessor } from '@/lib/api/professores';
import { useAutenticadoApple } from '@/lib/auth/useAutenticadoApple';
import { useAutenticadoGoogle } from '@/lib/auth/useAutenticadoGoogle';
import { useCadastroUsuario } from '@/lib/useCadastroUsuario';
import { MaxContentWidth } from '@/theme/tokens';

/**
 * Tela de cadastro de Professor (issue #1). Após sucesso, mostra uma
 * confirmação inline — o login por código acontece depois (issue #18).
 *
 * Também aceita a entrada com conta Google (issue #65) e Apple (issue
 * #212): o `?email=` da rota (vindo do login quando o cadastro está
 * pendente) pré-preenche o campo de contato via `contatoInicial` — sem
 * sobrescrever o que o usuário digitar depois. Quando o
 * `BotaoLoginGoogle`/`BotaoLoginApple` bem-sucedido encontra um usuário já
 * existente, `useAutenticadoGoogle`/`useAutenticadoApple` (compartilhados
 * com Home/Login, issues #121/#212) persistem a sessão e navegam pra
 * `/painel`, com o mesmo tratamento de falha ao gravar no dispositivo;
 * quando o e-mail ainda não tem conta, o próprio cadastro é o destino,
 * então só pré-preenche o contato com o e-mail do Google/Apple.
 */
export default function CadastroProfessorScreen() {
  const { email } = useLocalSearchParams<{ email?: string }>();
  const { handleAutenticadoGoogle, erroGoogle } = useAutenticadoGoogle();
  const { handleAutenticadoApple, erroApple } = useAutenticadoApple();
  const {
    nome,
    contato,
    erro,
    enviando,
    concluido,
    nomeReadonly,
    setContato,
    setNome,
    handleChangeContato,
    handleBlurContato,
    handleSubmit,
  } = useCadastroUsuario(
    { cadastrar: cadastrarProfessor, verificarContato: verificarContatoProfessor },
    { contatoInicial: email },
  );

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
          <>
            <CadastroUsuarioForm
              nome={nome}
              contato={contato}
              erro={erro ?? erroGoogle ?? erroApple}
              enviando={enviando}
              nomeReadonly={nomeReadonly}
              onChangeNome={setNome}
              onChangeContato={handleChangeContato}
              onBlurContato={handleBlurContato}
              onSubmit={handleSubmit}
            />
            <BotaoLoginGoogle
              onAutenticado={handleAutenticadoGoogle}
              onCadastroPendente={setContato}
            />
            <BotaoLoginApple
              onAutenticado={handleAutenticadoApple}
              onCadastroPendente={setContato}
            />
          </>
        )}
      </View>
    </SafeAreaView>
  );
}
