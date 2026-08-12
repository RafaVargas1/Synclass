import { useRouter } from 'expo-router';
import { useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { SolicitarCodigoForm } from '@/components/organisms/SolicitarCodigoForm';
import { solicitarCodigo } from '@/lib/api/auth';

/**
 * Tela de solicitação de login por código (issue #18): pede o contato e, em
 * caso de sucesso, navega para a tela de verificação (app/login/verificar.tsx)
 * levando o contato — só ela sabe para quem o código foi enviado.
 */
export default function LoginScreen() {
  const router = useRouter();
  const [contato, setContato] = useState('');
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [enviando, setEnviando] = useState(false);

  async function handleSubmit() {
    setEnviando(true);
    setErro(undefined);

    const resultado = await solicitarCodigo({ contato });

    setEnviando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    router.push({ pathname: '/login/verificar', params: { contato } });
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <View className="flex-1 items-center justify-center px-four">
        <SolicitarCodigoForm
          contato={contato}
          erro={erro}
          enviando={enviando}
          onChangeContato={setContato}
          onSubmit={handleSubmit}
        />
      </View>
    </SafeAreaView>
  );
}
