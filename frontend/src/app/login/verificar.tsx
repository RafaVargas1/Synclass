import { useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { LoginConfirmado } from '@/components/molecules/LoginConfirmado';
import { VerificarCodigoForm } from '@/components/organisms/VerificarCodigoForm';
import { confirmarCodigo, solicitarCodigo } from '@/lib/api/auth';
import { salvarToken } from '@/lib/auth/sessao';

/**
 * Tela de confirmação do código OTP (issue #18). Recebe o contato da tela
 * anterior (app/login/index.tsx) via parâmetro de rota — só ela sabe para
 * quem o código foi enviado, então não pede o contato de novo.
 */
export default function VerificarCodigoScreen() {
  const { contato } = useLocalSearchParams<{ contato: string }>();
  const [codigo, setCodigo] = useState('');
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [enviando, setEnviando] = useState(false);
  const [nomeConfirmado, setNomeConfirmado] = useState<string | undefined>(undefined);

  async function handleSubmit() {
    setEnviando(true);
    setErro(undefined);

    const resultado = await confirmarCodigo({ contato, codigo });

    setEnviando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    await salvarToken(resultado.token);
    setNomeConfirmado(resultado.nome);
  }

  async function handleReenviar() {
    setErro(undefined);
    await solicitarCodigo({ contato });
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <View className="flex-1 items-center justify-center px-four">
        {nomeConfirmado ? (
          <LoginConfirmado nome={nomeConfirmado} />
        ) : (
          <VerificarCodigoForm
            codigo={codigo}
            erro={erro}
            enviando={enviando}
            onChangeCodigo={setCodigo}
            onSubmit={handleSubmit}
            onReenviar={handleReenviar}
          />
        )}
      </View>
    </SafeAreaView>
  );
}
