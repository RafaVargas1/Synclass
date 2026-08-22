import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Topbar } from '@/components/organisms/Topbar';
import { VerificarCodigoForm } from '@/components/organisms/VerificarCodigoForm';
import { confirmarCodigo, solicitarCodigo } from '@/lib/api/auth';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { MaxContentWidth } from '@/theme/tokens';

/**
 * Tela de confirmação do código OTP (issue #18). Recebe o contato da tela
 * anterior (app/login/index.tsx) via parâmetro de rota — só ela sabe para
 * quem o código foi enviado, então não pede o contato de novo. Em caso de
 * sucesso, persiste token e papéis via `useSessao().definirSessao` (issue
 * #4) — nunca grava direto em `lib/auth/sessao.ts`, para o `SessaoProvider`
 * já saber da sessão nova antes de navegar para `/painel` (ver comentário em
 * `contexto-sessao.tsx`) — e navega para a área logada; não há mais
 * confirmação estática nesta tela.
 */
export default function VerificarCodigoScreen() {
  const { contato } = useLocalSearchParams<{ contato: string }>();
  const router = useRouter();
  const { definirSessao } = useSessao();
  const [codigo, setCodigo] = useState('');
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [enviando, setEnviando] = useState(false);

  async function handleSubmit() {
    setEnviando(true);
    setErro(undefined);

    const resultado = await confirmarCodigo({ contato, codigo });

    if (!resultado.sucesso) {
      setEnviando(false);
      setErro(resultado.mensagem);
      return;
    }

    try {
      await definirSessao(resultado.token, resultado.papeis);
    } catch {
      setEnviando(false);
      setErro('Não foi possível concluir o login neste dispositivo. Tente novamente.');
      return;
    }
    setEnviando(false);
    router.replace('/painel');
  }

  async function handleReenviar() {
    setErro(undefined);
    await solicitarCodigo({ contato });
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <Topbar titulo="Confirmar código" />
      <View
        className="w-full flex-1 self-center items-center justify-center px-four"
        style={{ maxWidth: MaxContentWidth }}
      >
        <VerificarCodigoForm
          codigo={codigo}
          erro={erro}
          enviando={enviando}
          onChangeCodigo={setCodigo}
          onSubmit={handleSubmit}
          onReenviar={handleReenviar}
        />
      </View>
    </SafeAreaView>
  );
}
