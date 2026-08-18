import { useRouter } from 'expo-router';
import { useEffect } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Heading } from '@/components/atoms/Heading';
import { Paragraph } from '@/components/atoms/Paragraph';
import { AlternadorDePapel } from '@/components/organisms/AlternadorDePapel';
import { useSessao } from '@/lib/auth/contexto-sessao';

/**
 * Ações disponíveis por papel — placeholder textual (issue #4): as telas
 * reais de cada ação ("criar horário", "ver meus Alunos" etc.) já existem
 * ou são objeto de issues futuras, não desta (ver implementation.md).
 */
const AcoesPorPapel: Record<string, string[]> = {
  Professor: ['Gerenciar horários', 'Ver meus Alunos', 'Convidar Aluno'],
  Aluno: ['Ver meus horários', 'Marcar aula em horário vago'],
};

/**
 * Tela pós-login (issue #4): landing após confirmar o código OTP
 * (app/login/verificar.tsx). Redireciona para /login sem sessão salva —
 * guarda de rota mínima, já que ainda não há middleware de rota no Expo
 * Router — e alterna o conteúdo conforme o papel ativo quando o usuário
 * acumula mais de um papel.
 */
export default function PainelScreen() {
  const router = useRouter();
  const { carregando, token, papeis, papelAtivo, definirPapelAtivo } = useSessao();

  useEffect(() => {
    if (!carregando && !token) {
      router.replace('/login');
    }
  }, [carregando, token, router]);

  if (carregando || !token) {
    return null;
  }

  const acoes = papelAtivo ? (AcoesPorPapel[papelAtivo] ?? []) : [];

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <View className="flex-1 gap-four px-four py-four">
        <Heading level={1}>Painel</Heading>
        <AlternadorDePapel papeis={papeis} papelAtivo={papelAtivo} onSelecionarPapel={definirPapelAtivo} />
        <View className="gap-two">
          {acoes.map((acao) => (
            <Paragraph key={acao}>{acao}</Paragraph>
          ))}
        </View>
      </View>
    </SafeAreaView>
  );
}
