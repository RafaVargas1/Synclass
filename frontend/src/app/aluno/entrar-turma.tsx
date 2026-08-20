import { useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AceiteConviteConfirmado } from '@/components/molecules/AceiteConviteConfirmado';
import { ConviteExpirado } from '@/components/molecules/ConviteExpirado';
import { FormField } from '@/components/molecules/FormField';
import { CadastroUsuarioForm } from '@/components/organisms/CadastroUsuarioForm';
import { aceitarConvitePorCodigo } from '@/lib/api/convites';
import { normalizarCodigoConvite } from '@/lib/normalizarCodigoConvite';

/**
 * Reconhece a mensagem de convite expirado devolvida pela Api (ver
 * `ConviteExpiradoException` em Synclass.Domain.Convites), mesmo racional
 * de `app/convite/[token].tsx`.
 */
function ehConviteExpirado(mensagem: string): boolean {
  return mensagem.includes('expirou');
}

/**
 * Rota pública de entrada na turma pelo código curto de 5 dígitos (issue
 * #63) — alternativa ao link de `app/convite/[token].tsx` para o Aluno que
 * recebeu o código em vez do link (ex: passado verbalmente pelo Professor).
 * Reaproveita `CadastroUsuarioForm` (nome + contato) e os estados de
 * confirmação/expiração do fluxo por link, tal como pedido pelo card.
 */
export default function EntrarTurmaScreen() {
  const [codigo, setCodigo] = useState('');
  const [nome, setNome] = useState('');
  const [contato, setContato] = useState('');
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [enviando, setEnviando] = useState(false);
  const [nomeConfirmado, setNomeConfirmado] = useState<string | undefined>(undefined);
  const [expirado, setExpirado] = useState(false);

  async function handleSubmit() {
    setEnviando(true);
    setErro(undefined);

    const resultado = await aceitarConvitePorCodigo({
      codigo: normalizarCodigoConvite(codigo),
      nome,
      contato,
    });

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
          <View className="w-full gap-four">
            <FormField
              label="Código da turma"
              value={codigo}
              onChangeText={(texto) => setCodigo(normalizarCodigoConvite(texto))}
              placeholder="12345"
              keyboardType="number-pad"
              maxLength={5}
            />
            <CadastroUsuarioForm
              nome={nome}
              contato={contato}
              erro={erro}
              enviando={enviando}
              // O readonly de Nome por identidade já existente (issue #27) é
              // escopo do cadastro direto de Professor (app/professor/cadastro.tsx)
              // — o fluxo de entrada por código, assim como o de aceite por
              // link, não foi coberto por aquele card.
              nomeReadonly={false}
              onChangeNome={setNome}
              onChangeContato={setContato}
              onBlurContato={() => {}}
              onSubmit={handleSubmit}
            />
          </View>
        )}
      </View>
    </SafeAreaView>
  );
}
