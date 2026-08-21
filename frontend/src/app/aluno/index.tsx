import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { BotaoLoginGoogle } from '@/components/molecules/BotaoLoginGoogle';
import { AceiteConviteConfirmado } from '@/components/molecules/AceiteConviteConfirmado';
import { CadastroConfirmado } from '@/components/molecules/CadastroConfirmado';
import { ConviteExpirado } from '@/components/molecules/ConviteExpirado';
import { FormField } from '@/components/molecules/FormField';
import { Paragraph } from '@/components/atoms/Paragraph';
import { CadastroUsuarioForm } from '@/components/organisms/CadastroUsuarioForm';
import { Topbar } from '@/components/organisms/Topbar';
import { cadastrarAluno, verificarContatoAluno } from '@/lib/api/alunos';
import { aceitarConvitePorCodigo } from '@/lib/api/convites';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { normalizarCodigoConvite } from '@/lib/normalizarCodigoConvite';

/**
 * Reconhece a mensagem de convite expirado devolvida pela Api (ver
 * `ConviteExpiradoException` em Synclass.Domain.Convites), mesmo racional
 * de `app/convite/[token].tsx` e `app/aluno/entrar-turma.tsx` (issue #63).
 * Só faz sentido quando o envio usou código — cadastro independente não tem
 * conceito de expiração.
 */
function ehConviteExpirado(mensagem: string): boolean {
  return mensagem.includes('expirou');
}

/**
 * Tela única de entrada do Aluno (issue #64) — consolida o aceite de convite
 * por código (issue #63, antes em `app/aluno/entrar-turma.tsx`) e o cadastro
 * independente (issue #61, antes em `app/aluno/cadastro.tsx`) numa só tela,
 * sem alternância de rota/aba: o campo de código é opcional, então os dois
 * formulários ficam visíveis ao mesmo tempo. No submit decide qual cliente
 * chamar conforme o código (já normalizado) estar vazio ou não. Reaproveita
 * `CadastroUsuarioForm`, os estados de confirmação/expiração e a
 * normalização de código — sem duplicar formulário nem lógica.
 *
 * Também aceita a entrada com conta Google (issue #65): o `?email=` da rota
 * (vindo do login quando o cadastro está pendente) pré-preenche o campo de
 * contato — sem sobrescrever o que o usuário digitar depois. Quando o
 * `BotaoLoginGoogle` bem-sucedido encontra um usuário já existente, persiste
 * a sessão e vai para `/painel`; quando o e-mail ainda não tem conta, o
 * próprio cadastro é o destino, então só pré-preenche o contato com o e-mail
 * do Google.
 */
export default function AlunoScreen() {
  const { email } = useLocalSearchParams<{ email?: string }>();
  const router = useRouter();
  const { definirSessao } = useSessao();
  const [codigo, setCodigo] = useState('');
  const [nome, setNome] = useState('');
  const [contato, setContato] = useState(email ?? '');
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [enviando, setEnviando] = useState(false);
  const [nomeReadonly, setNomeReadonly] = useState(false);
  const [nomeConfirmado, setNomeConfirmado] = useState<string | undefined>(undefined);
  const [usouCodigo, setUsouCodigo] = useState(false);
  const [expirado, setExpirado] = useState(false);

  async function handleBlurContato() {
    const resultado = await verificarContatoAluno(contato);
    setNomeReadonly(resultado.identidadeExistente);
    if (resultado.identidadeExistente && resultado.nome) {
      setNome(resultado.nome);
    }
  }

  function handleChangeContato(contatoNovo: string) {
    setContato(contatoNovo);
    setNomeReadonly(false);
  }

  async function handleSubmit() {
    setEnviando(true);
    setErro(undefined);

    const resultado = codigo
      ? await aceitarConvitePorCodigo({ codigo, nome, contato })
      : await cadastrarAluno({ nome, contato });

    setEnviando(false);
    if (!resultado.sucesso) {
      if (codigo && ehConviteExpirado(resultado.mensagem)) {
        setExpirado(true);
        return;
      }
      setErro(resultado.mensagem);
      return;
    }
    setUsouCodigo(Boolean(codigo));
    setNomeConfirmado(resultado.nome);
  }

  async function handleAutenticado({
    token,
    papeis,
  }: {
    token: string;
    papeis: string[];
  }) {
    await definirSessao(token, papeis);
    router.replace('/painel');
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <Topbar titulo="Entrar como Aluno" />
      <View className="w-full flex-1 items-center justify-center self-center px-four gap-four">
        {expirado ? (
          <ConviteExpirado />
        ) : nomeConfirmado ? (
          usouCodigo ? (
            <AceiteConviteConfirmado nome={nomeConfirmado} />
          ) : (
            <CadastroConfirmado papel="Aluno" />
          )
        ) : (
          <View className="w-full gap-four">
            <FormField
              label="Código da turma (opcional)"
              value={codigo}
              onChangeText={(texto) => setCodigo(normalizarCodigoConvite(texto))}
              placeholder="12345"
              keyboardType="number-pad"
              maxLength={5}
            />
            <Paragraph>
              Tem o código de 5 dígitos que seu Professor te passou? Preencha acima. Sem código
              em mãos, só cadastre seu nome e contato abaixo — o vínculo com o Professor é feito
              depois, por código ou link.
            </Paragraph>
            <CadastroUsuarioForm
              nome={nome}
              contato={contato}
              erro={erro}
              enviando={enviando}
              nomeReadonly={nomeReadonly}
              onChangeNome={setNome}
              onChangeContato={handleChangeContato}
              onBlurContato={handleBlurContato}
              onSubmit={handleSubmit}
            />
            <BotaoLoginGoogle
              onAutenticado={handleAutenticado}
              onCadastroPendente={setContato}
            />
          </View>
        )}
      </View>
    </SafeAreaView>
  );
}
