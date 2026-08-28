import { useRouter } from 'expo-router';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Button } from '@/components/atoms/Button';
import { Paragraph } from '@/components/atoms/Paragraph';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import { MaxContentWidth } from '@/theme/tokens';

/**
 * Tela de retorno do checkout Mercado Pago indicando sucesso no pagamento
 * (issue #199): o navegador é redirecionado pra cá pela `back_url` de
 * `success` após o Aluno concluir o Checkout Pro. É tela mínima — só
 * mensagem fixa + link de volta pro valor devido, sem chamada de API: o
 * status real do pagamento só muda via webhook de #200 (estas telas não
 * confirmam nada por si, ver
 * implementation.md#frontend-telas-de-retorno-do-checkout). Mesmo padrão de
 * tela simples de `professor/[professorId]/configuracoes.tsx`.
 */
export default function PagamentoConfirmadoScreen() {
  const router = useRouter();

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <TopbarAutenticada titulo="Pagamento" />
      <View className="w-full flex-1 self-center gap-four px-four py-five" style={{ maxWidth: MaxContentWidth }}>
        <Paragraph>Pagamento concluído — o valor devido é atualizado assim que o pagamento for confirmado.</Paragraph>
        <Button label="Voltar para o valor devido" variante="secundario" onPress={() => router.push('/aluno/valor-devido')} />
      </View>
    </SafeAreaView>
  );
}
