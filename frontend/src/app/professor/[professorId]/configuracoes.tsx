import { useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { Linking, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Paragraph } from '@/components/atoms/Paragraph';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import { conectarMercadoPago } from '@/lib/api/mercadoPago';
import { MaxContentWidth } from '@/theme/tokens';

/**
 * Tela de Configurações do Professor (issue #203), mínima: só a seção de
 * integração de pagamento (conta Mercado Pago). Não é um hub de
 * configurações genérico (YAGNI) — se o produto pedir mais configurações,
 * cresce aqui depois. A tela não mostra status "conectado": o backend desta
 * Task não expõe endpoint de status; a confirmação de sucesso é a página
 * HTML do próprio callback do Mercado Pago (ver
 * implementation.md#resposta-do-callback).
 */
export default function ConfiguracoesScreen() {
  const { professorId } = useLocalSearchParams<{ professorId: string }>();
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [conectando, setConectando] = useState(false);

  async function aoConectarMercadoPago() {
    setErro(undefined);
    setConectando(true);
    const resultado = await conectarMercadoPago(professorId);
    setConectando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    Linking.openURL(resultado.url);
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <TopbarAutenticada titulo="Configurações" />
      <View className="w-full flex-1 self-center gap-four px-four py-five" style={{ maxWidth: MaxContentWidth }}>
        <Paragraph>Conecte sua conta do Mercado Pago para receber os pagamentos dos seus Alunos diretamente, sem repasse manual.</Paragraph>
        <Button
          label="Conectar conta do Mercado Pago"
          variante="secundario"
          disabled={conectando}
          onPress={aoConectarMercadoPago}
        />
        {erro ? <ErrorMessage>{erro}</ErrorMessage> : null}
      </View>
    </SafeAreaView>
  );
}
