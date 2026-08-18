import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { VerificarCodigoForm } from '@/components/organisms/VerificarCodigoForm';
import { confirmarCodigo, solicitarCodigo } from '@/lib/api/auth';
import { salvarPapeis, salvarToken } from '@/lib/auth/sessao';

/**
 * Tela de confirmação do código OTP (issue #18). Recebe o contato da tela
 * anterior (app/login/index.tsx) via parâmetro de rota — só ela sabe para
 * quem o código foi enviado, então não pede o contato de novo. Em caso de
 * sucesso, persiste token e papéis e navega para a área logada (issue #4,
 * `app/painel`) — não há mais confirmação estática nesta tela.
 */
export default function VerificarCodigoScreen() {
  const { contato } = useLocalSearchParams<{ contato: string }>();
  const router = useRouter();
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
      await salvarToken(resultado.token);
      await salvarPapeis(resultado.papeis);
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
      <View className="flex-1 items-center justify-center px-four">
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
