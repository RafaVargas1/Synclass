import { useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { Linking, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ConviteGerado } from '@/components/molecules/ConviteGerado';
import { GerarConviteForm } from '@/components/organisms/GerarConviteForm';
import { Topbar } from '@/components/organisms/Topbar';
import { gerarConvite } from '@/lib/api/convites';
import { montarLinkWhatsApp } from '@/lib/whatsapp';

/**
 * URL base do app (Expo web), usada para montar o link público de aceite
 * compartilhado com o Aluno — mesmo padrão de `EXPO_PUBLIC_API_URL` em
 * `lib/api/httpClient.ts`, mas para a origem do próprio frontend, não da Api.
 */
const AppBaseUrl = process.env.EXPO_PUBLIC_APP_URL ?? 'http://localhost:8081';

/**
 * Tela do Professor para gerar convite e compartilhar via WhatsApp (issue
 * #2). `professorId` vem da rota (`[professorId]`), mesma decisão de
 * `alunos/cadastro.tsx` — ver docs/specs/2-convite-whatsapp/implementation.md.
 */
export default function GerarConviteScreen() {
  // `matriculaId` é opcional: presente quando o link chega a partir da tela
  // de um Aluno provisório específico (item 3 do backlog), para o convite
  // já nascer com a referência à matrícula de origem (RN da issue #2,
  // critério de aceite 5) — nunca digitado pelo Professor.
  const { professorId, matriculaId } = useLocalSearchParams<{
    professorId: string;
    matriculaId?: string;
  }>();
  const [contato, setContato] = useState('');
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [enviando, setEnviando] = useState(false);
  const [linkConvite, setLinkConvite] = useState<string | undefined>(undefined);

  async function handleSubmit() {
    setEnviando(true);
    setErro(undefined);

    const resultado = await gerarConvite({ professorId, contato, matriculaId });

    setEnviando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setLinkConvite(`${AppBaseUrl}/convite/${resultado.token}`);
  }

  function handleEnviarWhatsApp() {
    if (!linkConvite) {
      return;
    }
    const mensagem = `Você foi convidado para o Synclass! Complete seu cadastro: ${linkConvite}`;
    Linking.openURL(montarLinkWhatsApp(contato, mensagem));
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <Topbar titulo="Convidar Aluno" />
      <View className="flex-1 items-center justify-center px-four">
        {linkConvite ? (
          <ConviteGerado linkConvite={linkConvite} onEnviarWhatsApp={handleEnviarWhatsApp} />
        ) : (
          <GerarConviteForm
            contato={contato}
            erro={erro}
            enviando={enviando}
            onChangeContato={setContato}
            onSubmit={handleSubmit}
          />
        )}
      </View>
    </SafeAreaView>
  );
}
