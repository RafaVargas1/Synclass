import { useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { ActivityIndicator, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Heading } from '@/components/atoms/Heading';
import { Paragraph } from '@/components/atoms/Paragraph';
import { RegraDeCobrancaForm } from '@/components/organisms/RegraDeCobrancaForm';
import {
  definirRegraDeCobranca,
  obterRegraDeCobranca,
  type DefinirRegraDeCobrancaInput,
  type RegraDeCobranca,
} from '@/lib/api/regraDeCobranca';

/**
 * Tela de definição da regra de cobrança de uma matrícula (issue #11).
 * `professorId`/`matriculaId` vêm da rota, mesmo padrão de
 * `alunos/cadastro.tsx` — ainda não há sessão logada (issue #18, em
 * paralelo) de onde derivar o Professor autenticado. Consulta `GET
 * regra-de-cobranca` ao montar (mesmo gate de `horarios.tsx`/issue #7) para
 * pré-preencher o formulário quando já existe uma regra — sem isso, reabrir
 * a tela sobrescrevia a regra vigente em silêncio via upsert (achado de
 * dev-review, rodada 1 do PR #31).
 */
export default function RegraDeCobrancaScreen() {
  const { professorId, matriculaId } = useLocalSearchParams<{ professorId: string; matriculaId: string }>();
  const carregamento = useCarregamentoRegra(professorId, matriculaId);

  if (carregamento.status === 'carregando') {
    return <TelaCarregando />;
  }
  if (carregamento.status === 'falha') {
    return (
      <TelaErroCarregamento mensagem={carregamento.mensagem} onTentarNovamente={carregamento.tentarNovamente} />
    );
  }
  return (
    <TelaComFormulario professorId={professorId} matriculaId={matriculaId} regraExistente={carregamento.regra} />
  );
}

function TelaComFormulario({
  professorId,
  matriculaId,
  regraExistente,
}: {
  professorId: string;
  matriculaId: string;
  regraExistente?: RegraDeCobranca;
}) {
  const { enviando, erro, salva, handleSubmit } = useDefinirRegraDeCobranca(professorId, matriculaId);
  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <View className="flex-1 items-center justify-center gap-four px-four">
        <Heading level={1}>Regra de cobrança</Heading>
        {salva ? (
          <Paragraph>Regra de cobrança salva!</Paragraph>
        ) : (
          <RegraDeCobrancaForm
            enviando={enviando}
            erro={erro}
            regraExistente={regraExistente}
            onSubmit={handleSubmit}
          />
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

type ResultadoCarregamento =
  { sucesso: true; regra?: RegraDeCobranca } | { sucesso: false; mensagem: string };

type EstadoCarregamento =
  | { status: 'carregando' }
  | { status: 'falha'; mensagem: string; tentarNovamente: () => void }
  | { status: 'carregada'; regra?: RegraDeCobranca };

/**
 * Consulta `GET regra-de-cobranca` ao montar. 404 (sem regra configurada)
 * não é falha — vira `regra: undefined`, e o formulário abre com os
 * defaults, mesma distinção de `obterRegraDeCobranca` (ver
 * lib/api/regraDeCobranca.ts).
 */
function useCarregamentoRegra(professorId: string, matriculaId: string): EstadoCarregamento {
  const [resultado, setResultado] = useState<ResultadoCarregamento | undefined>(undefined);
  const [tentativa, setTentativa] = useState(0);
  const tentarNovamente = () => {
    setResultado(undefined);
    setTentativa((atual) => atual + 1);
  };

  useEffect(() => {
    let cancelado = false;
    obterRegraDeCobranca(professorId, matriculaId).then((res) => {
      if (cancelado) return;
      setResultado(res.sucesso ? { sucesso: true, regra: res.definida ? res.regra : undefined } : res);
    });
    return () => {
      cancelado = true;
    };
  }, [professorId, matriculaId, tentativa]);

  if (resultado === undefined) {
    return { status: 'carregando' };
  }
  if (!resultado.sucesso) {
    return { status: 'falha', mensagem: resultado.mensagem, tentarNovamente };
  }
  return { status: 'carregada', regra: resultado.regra };
}

function TelaCarregando() {
  return (
    <SafeAreaView className="flex-1 items-center justify-center bg-background dark:bg-dark-background">
      <ActivityIndicator accessibilityLabel="Carregando" />
    </SafeAreaView>
  );
}

function TelaErroCarregamento({
  mensagem,
  onTentarNovamente,
}: {
  mensagem: string;
  onTentarNovamente: () => void;
}) {
  return (
    <SafeAreaView className="flex-1 items-center justify-center gap-four bg-background px-four dark:bg-dark-background">
      <ErrorMessage>{mensagem}</ErrorMessage>
      <Button label="Tentar novamente" onPress={onTentarNovamente} />
    </SafeAreaView>
  );
}
