import { useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Heading } from '@/components/atoms/Heading';
import { Paragraph } from '@/components/atoms/Paragraph';
import { RegraDeCobrancaForm } from '@/components/organisms/RegraDeCobrancaForm';
import { definirRegraDeCobranca, type DefinirRegraDeCobrancaInput } from '@/lib/api/regraDeCobranca';

/**
 * Tela de definição da regra de cobrança de uma matrícula (issue #11).
 * `professorId`/`matriculaId` vêm da rota, mesmo padrão de
 * `alunos/cadastro.tsx` — ainda não há sessão logada (issue #18, em
 * paralelo) de onde derivar o Professor autenticado.
 */
export default function RegraDeCobrancaScreen() {
  const { professorId, matriculaId } = useLocalSearchParams<{ professorId: string; matriculaId: string }>();
  const { enviando, erro, salva, handleSubmit } = useDefinirRegraDeCobranca(professorId, matriculaId);

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <View className="flex-1 items-center justify-center gap-four px-four">
        <Heading level={1}>Regra de cobrança</Heading>
        {salva ? (
          <Paragraph>Regra de cobrança salva!</Paragraph>
        ) : (
          <RegraDeCobrancaForm enviando={enviando} erro={erro} onSubmit={handleSubmit} />
        )}
      </View>
    </SafeAreaView>
  );
}

function useDefinirRegraDeCobranca(professorId: string, matriculaId: string) {
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [salva, setSalva] = useState(false);

  async function handleSubmit(input: DefinirRegraDeCobrancaInput) {
    setEnviando(true);
    setErro(undefined);

    const resultado = await definirRegraDeCobranca(professorId, matriculaId, input);

    setEnviando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setSalva(true);
  }

  return { enviando, erro, salva, handleSubmit };
}
