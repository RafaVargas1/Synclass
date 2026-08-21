import { useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { Linking, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ConviteGerado } from '@/components/molecules/ConviteGerado';
import { GerarConviteForm } from '@/components/organisms/GerarConviteForm';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import { gerarConvite } from '@/lib/api/convites';
import { montarLinkWhatsApp } from '@/lib/whatsapp';
import { MaxContentWidth } from '@/theme/tokens';

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
  const [codigoConvite, setCodigoConvite] = useState<string | undefined>(undefined);

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
    setCodigoConvite(resultado.codigo);
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
      <TopbarAutenticada titulo="Convidar Aluno" />
      <View
        className="w-full flex-1 self-center items-center justify-center px-four"
        style={{ maxWidth: MaxContentWidth }}
      >
        {linkConvite && codigoConvite ? (
          <ConviteGerado
            linkConvite={linkConvite}
            codigo={codigoConvite}
            onEnviarWhatsApp={handleEnviarWhatsApp}
          />
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
