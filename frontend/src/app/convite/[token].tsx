import { useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AceiteConviteConfirmado } from '@/components/molecules/AceiteConviteConfirmado';
import { ConviteExpirado } from '@/components/molecules/ConviteExpirado';
import { CadastroProfessorForm } from '@/components/organisms/CadastroProfessorForm';
import { aceitarConvite } from '@/lib/api/convites';

/**
 * Reconhece a mensagem de convite expirado devolvida pela Api (ver
 * `ConviteExpiradoException` em Synclass.Domain.Convites) para mostrar um
 * estado dedicado — reenviar os mesmos dados nunca resolve, diferente dos
 * demais erros de negócio (ex: contato divergente), que ficam inline no
 * formulário.
 */
function ehConviteExpirado(mensagem: string): boolean {
  return mensagem.includes('expirou');
}

/**
 * Rota pública de aceite de convite (issue #2), reaproveitando
 * `CadastroProfessorForm` (nome + contato) tal como pedido pelo card — ver
 * decisão documentada em docs/specs/2-convite-whatsapp/implementation.md.
 * `token` vem da rota dinâmica (`[token]`).
 */
export default function AceiteConviteScreen() {
  const { token } = useLocalSearchParams<{ token: string }>();
  const [nome, setNome] = useState('');
  const [contato, setContato] = useState('');
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [enviando, setEnviando] = useState(false);
  const [nomeConfirmado, setNomeConfirmado] = useState<string | undefined>(undefined);
  const [expirado, setExpirado] = useState(false);

  async function handleSubmit() {
    setEnviando(true);
    setErro(undefined);

    const resultado = await aceitarConvite({ token, nome, contato });

    setEnviando(false);
    if (resultado.sucesso) {
      setNomeConfirmado(resultado.nome);
      return;
    }
    if (ehConviteExpirado(resultado.mensagem)) {
      setExpirado(true);
      return;
    }
    setErro(resultado.mensagem);
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <View className="flex-1 items-center justify-center px-four">
        {expirado ? (
          <ConviteExpirado />
        ) : nomeConfirmado ? (
          <AceiteConviteConfirmado nome={nomeConfirmado} />
        ) : (
          <CadastroProfessorForm
            nome={nome}
            contato={contato}
            erro={erro}
            enviando={enviando}
            onChangeNome={setNome}
            onChangeContato={setContato}
            onSubmit={handleSubmit}
          />
        )}
      </View>
    </SafeAreaView>
  );
}
