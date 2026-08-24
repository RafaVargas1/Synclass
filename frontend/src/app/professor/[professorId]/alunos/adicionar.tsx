import { useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { Linking, Pressable, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Paragraph } from '@/components/atoms/Paragraph';
import { AlunoProvisorioConfirmado } from '@/components/molecules/AlunoProvisorioConfirmado';
import { ConviteGerado } from '@/components/molecules/ConviteGerado';
import { CadastroAlunoProvisorioForm } from '@/components/organisms/CadastroAlunoProvisorioForm';
import { GerarConviteForm } from '@/components/organisms/GerarConviteForm';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import { cadastrarAlunoProvisorio } from '@/lib/api/alunosProvisorios';
import { gerarCodigoEntradaTurma } from '@/lib/api/codigosEntradaTurma';
import { gerarConvite } from '@/lib/api/convites';
import { formatarContagem, useContagemRegressiva } from '@/lib/useContagemRegressiva';
import { montarLinkWhatsApp } from '@/lib/whatsapp';
import { AlvoDeToqueMinimo, MaxContentWidth } from '@/theme/tokens';

/**
 * URL base do app (Expo web), usada para montar o link público de aceite
 * compartilhado com o Aluno — mesmo padrão de `EXPO_PUBLIC_API_URL` em
 * `lib/api/httpClient.ts`, mas para a origem do próprio frontend, não da Api.
 */
const AppBaseUrl = process.env.EXPO_PUBLIC_APP_URL ?? 'http://localhost:8081';

type Modo = 'sem-contato' | 'convite' | 'codigo-turma';

const AbasModo: { modo: Modo; label: string }[] = [
  { modo: 'sem-contato', label: 'Sem contato' },
  { modo: 'convite', label: 'Por convite' },
  { modo: 'codigo-turma', label: 'Código da turma' },
];

/**
 * "Adicionar Aluno" — porta de entrada única do Professor pra trazer um
 * Aluno pra dentro do próprio cadastro, substituindo as telas separadas
 * "Cadastrar Aluno" e "Convidar Aluno". Três modos, uma aba cada, mesma
 * regra de negócio de antes por trás (não é uma fusão de regra, só de
 * navegação — ver docs/spec/business-rules.md, "Convites"): cadastro direto
 * sem contato (Aluno provisório), convite direcionado por contato
 * (`Convite`, uso único) e o código de entrada de turma (curto, multiuso,
 * só pra Aluno já autenticado). Se a rota chegar com `matriculaId` (convite
 * a partir de um Aluno provisório específico), abre direto na aba "Por
 * convite" — é o único modo que usa esse parâmetro.
 */
export default function AdicionarAlunoScreen() {
  const { professorId, matriculaId } = useLocalSearchParams<{
    professorId: string;
    matriculaId?: string;
  }>();
  const [modo, setModo] = useState<Modo>(matriculaId ? 'convite' : 'sem-contato');

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <TopbarAutenticada titulo="Adicionar Aluno" />
      <View
        className="w-full flex-1 items-center self-center gap-four px-four py-four"
        style={{ maxWidth: MaxContentWidth }}
      >
        <SeletorDeModo modo={modo} onSelecionar={setModo} />
        {modo === 'sem-contato' ? <ModoSemContato /> : null}
        {modo === 'convite' ? <ModoConvite professorId={professorId} matriculaId={matriculaId} /> : null}
        {modo === 'codigo-turma' ? <ModoCodigoTurma professorId={professorId} /> : null}
      </View>
    </SafeAreaView>
  );
}

function SeletorDeModo({ modo, onSelecionar }: { modo: Modo; onSelecionar: (modo: Modo) => void }) {
  return (
    <View testID="abas-adicionar-aluno" accessibilityRole="tablist" className="w-full flex-row flex-wrap gap-one">
      {AbasModo.map((aba) => (
        <AbaDeModo
          key={aba.modo}
          label={aba.label}
          selecionado={aba.modo === modo}
          onPress={() => onSelecionar(aba.modo)}
        />
      ))}
    </View>
  );
}

function AbaDeModo({ label, selecionado, onPress }: { label: string; selecionado: boolean; onPress: () => void }) {
  const estilo = selecionado
    ? 'border-primary bg-primary dark:border-dark-primary dark:bg-dark-primary'
    : 'border-background-selected bg-background-element dark:border-dark-background-selected dark:bg-dark-background-element';
  const corDoTexto = selecionado ? 'text-background dark:text-dark-background' : 'text-text dark:text-dark-text';

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ selected: selecionado }}
      onPress={onPress}
      className={`items-center justify-center rounded-small border px-three py-two ${estilo}`}
      style={AlvoDeToqueMinimo}
    >
      <Text className={`font-medium ${corDoTexto}`}>{label}</Text>
    </Pressable>
  );
}

/** Aba "Sem contato": lógica idêntica à antiga `professor/alunos/cadastro.tsx`. */
function ModoSemContato() {
  const [nome, setNome] = useState('');
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [enviando, setEnviando] = useState(false);
  const [confirmado, setConfirmado] = useState<{ nome: string; identificador: string } | undefined>(
    undefined,
  );

  async function handleSubmit() {
    setEnviando(true);
    setErro(undefined);
    const resultado = await cadastrarAlunoProvisorio({ nome });
    setEnviando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setConfirmado({ nome: resultado.nome, identificador: resultado.identificador });
  }

  if (confirmado) {
    return <AlunoProvisorioConfirmado nome={confirmado.nome} identificador={confirmado.identificador} />;
  }
  return (
    <CadastroAlunoProvisorioForm
      nome={nome}
      erro={erro}
      enviando={enviando}
      onChangeNome={setNome}
      onSubmit={handleSubmit}
    />
  );
}

/** Aba "Por convite": lógica idêntica à antiga `professor/[professorId]/convites/novo.tsx`. */
function ModoConvite({ professorId, matriculaId }: { professorId: string; matriculaId?: string }) {
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

  if (linkConvite && codigoConvite) {
    return <ConviteGerado linkConvite={linkConvite} codigo={codigoConvite} onEnviarWhatsApp={handleEnviarWhatsApp} />;
  }
  return (
    <GerarConviteForm
      contato={contato}
      erro={erro}
      enviando={enviando}
      onChangeContato={setContato}
      onSubmit={handleSubmit}
    />
  );
}

const MensagemCodigoTurmaExplicacao =
  'Qualquer Aluno logado que digitar este código em até 5 minutos entra na turma — diferente do convite por contato, pode ser usado por vários Alunos ao mesmo tempo.';
const MensagemCodigoExpirado = 'Código expirado — gere um novo.';

/** Aba "Código da turma": nova, ativa o esquema de código de entrada. */
function ModoCodigoTurma({ professorId }: { professorId: string }) {
  const [codigo, setCodigo] = useState<string | undefined>(undefined);
  const [expiraEm, setExpiraEm] = useState<string | undefined>(undefined);
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [gerando, setGerando] = useState(false);
  const segundosRestantes = useContagemRegressiva(expiraEm);
  const expirado = codigo !== undefined && segundosRestantes <= 0;

  async function handleGerar() {
    setGerando(true);
    setErro(undefined);
    const resultado = await gerarCodigoEntradaTurma(professorId);
    setGerando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setCodigo(resultado.codigo);
    setExpiraEm(resultado.expiraEm);
  }

  return (
    <View className="w-full items-center gap-four">
      <Paragraph>{MensagemCodigoTurmaExplicacao}</Paragraph>
      {codigo && !expirado ? (
        <View accessibilityRole="alert" className="items-center gap-two">
          <Text className="text-4xl font-bold tracking-widest text-text dark:text-dark-text">{codigo}</Text>
          <Text className="text-text-secondary dark:text-dark-text-secondary">
            Expira em {formatarContagem(segundosRestantes)}
          </Text>
        </View>
      ) : null}
      {expirado ? (
        <Text className="text-text-secondary dark:text-dark-text-secondary">{MensagemCodigoExpirado}</Text>
      ) : null}
      {erro ? <ErrorMessage>{erro}</ErrorMessage> : null}
      <Button
        label={gerando ? 'Gerando...' : codigo ? 'Gerar novo código' : 'Gerar código'}
        onPress={handleGerar}
        disabled={gerando}
      />
    </View>
  );
}
