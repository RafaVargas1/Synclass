import { Link } from 'expo-router';
import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Heading } from '@/components/atoms/Heading';
import { ResumoFrequenciaCard } from '@/components/organisms/ResumoFrequenciaCard';
import { ResumoProximoHorario, type ProximoHorario } from '@/components/organisms/ResumoProximoHorario';
import { ResumoValorReceber } from '@/components/organisms/ResumoValorReceber';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import { listarProximasAulas, type AulaProxima } from '@/lib/api/cancelamentos';
import {
  calcularPeriodoUltimosNDias,
  listarHistoricoFrequenciaDoAluno,
  type HistoricoFrequenciaPorProfessor,
} from '@/lib/api/historicoFrequencia';
import { listarValorDevido, type ListarValorDevidoResultado } from '@/lib/api/valorDevido';
import { listarVinculosAluno, type VinculoProfessor } from '@/lib/api/vinculosAluno';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { useRedirecionarSemSessao } from '@/lib/auth/useRedirecionarSemSessao';
import { periodoDoDia, saudacaoPorPeriodo } from '@/lib/periodoDoDia';
import { secoesDoPapel, type Secao } from '@/lib/secoesPorPapel';
import { usePerfilLogado } from '@/lib/usePerfilLogado';
import { MaxContentWidthPainel } from '@/theme/tokens';

/**
 * Saudação de topo do Painel (issue #69): `"{Saudação}, {nome}"`, com o
 * período derivado da hora atual via `periodoDoDia`. Só renderiza quando o
 * `nome` do perfil resolve via `GET /usuarios/me` — antes disso não há o
 * que cumprimentar.
 */
function Saudacao({ nome }: { nome: string }) {
  const saudacao = saudacaoPorPeriodo[periodoDoDia(new Date().getHours())];
  return <Heading>{`${saudacao}, ${nome}`}</Heading>;
}

/**
 * Busca vínculos do Aluno (`GET /alunos/professores`, issue #165) e agrega
 * por todos eles as próximas aulas (`GET /professores/{id}/horarios/proximas-aulas`)
 * até chegar no `ProximoHorario` mais próximo no tempo. Não bloqueia o
 * Painel: erro de qualquer chamada cai no estado vazio (`null`), mesmo
 * tratamento não-bloqueante de `ResumoValorReceber` (issue #166).
 */
function useProximoHorario(habilitado: boolean, token: string | null): ProximoHorario | null {
  const [proximoHorario, setProximoHorario] = useState<ProximoHorario | null>(null);

  useEffect(() => {
    if (!habilitado || !token) {
      return;
    }
    let cancelado = false;
    void (async () => {
      const vinculosResultado = await listarVinculosAluno();
      if (cancelado || !vinculosResultado.sucesso) {
        return;
      }
      // Em paralelo (achado de dev-review, PR #176): um Aluno com N
      // Professores pagava N× a latência de rede em série antes disso.
      const resultadosPorVinculo = await Promise.all(
        vinculosResultado.vinculos.map((vinculo) => listarProximasAulas(vinculo.professorId)),
      );
      if (cancelado) {
        return;
      }
      const aulasPorProfessor = new Map<string, AulaProxima[]>();
      vinculosResultado.vinculos.forEach((vinculo, indice) => {
        const aulasResultado = resultadosPorVinculo[indice];
        if (aulasResultado.sucesso) {
          aulasPorProfessor.set(vinculo.professorId, aulasResultado.aulas);
        }
      });
      setProximoHorario(agregarProximoHorario(vinculosResultado.vinculos, aulasPorProfessor));
    })();
    return () => {
      cancelado = true;
    };
  }, [habilitado, token]);

  return proximoHorario;
}

/** Data+hora de uma aula em formato ordenável (`YYYY-MM-DDTHH:MM:SS`). */
function dataHoraDaAula(aula: AulaProxima): string {
  return `${aula.data}T${aula.horaInicio}`;
}

/**
 * Reduz vínculos + aulas já buscadas pela tela no único `ProximoHorario`
 * (a aula mais próxima no tempo entre todos os Professores), ou `null`
 * quando não há vínculo nem aula futura. A comparação lexicográfica de
 * `YYYY-MM-DDTHH:MM:SS` é suficiente porque `data` chega em `YYYY-MM-DD`
 * e `horaInicio` em `HH:MM:SS` (confirmado no contrato de `AulaProxima`).
 */
function agregarProximoHorario(
  vinculos: VinculoProfessor[],
  aulasPorProfessor: Map<string, AulaProxima[]>,
): ProximoHorario | null {
  let maisProxima: { aula: AulaProxima; professorNome: string } | null = null;
  for (const vinculo of vinculos) {
    for (const aula of aulasPorProfessor.get(vinculo.professorId) ?? []) {
      if (!maisProxima || dataHoraDaAula(aula) < dataHoraDaAula(maisProxima.aula)) {
        maisProxima = { aula, professorNome: vinculo.nome };
      }
    }
  }
  if (!maisProxima) {
    return null;
  }
  return {
    professorNome: maisProxima.professorNome,
    data: maisProxima.aula.data,
    horaInicio: maisProxima.aula.horaInicio,
  };
}

/**
 * Tela pós-login (issue #4): landing após confirmar o código OTP
 * (app/login/verificar.tsx). O corpo mostra as ações do papel ativo como
 * cards — o objetivo real do app pro usuário que acabou de entrar é agir
 * (marcar horário, ver frequência, ver quanto vai receber), não navegar até
 * um menu escondido pra descobrir o que dá pra fazer (Nielsen #1,
 * visibilidade do que o sistema oferece). `MenuNavegacao` (na
 * `TopbarAutenticada`) continua disponível em toda tela pra navegar embora
 * daqui, mas o Painel não depende dele pra mostrar as próprias ações —
 * evita a tela ficar vazia com só uma saudação e um botão solto (achado de
 * UX reportado pelo usuário: um CTA de "Meu perfil" do tamanho de ação
 * primária numa tela sem mais nada, quando o próprio menu já tem esse link
 * — ver `SecaoMeuPerfil` em `MenuNavegacao.tsx`).
 */
export default function PainelScreen() {
  const { carregando, token, papelAtivo } = useSessao();
  useRedirecionarSemSessao(carregando, token);
  const { usuarioId, nome } = usePerfilLogado(token);
  const proximoHorario = useProximoHorario(papelAtivo === 'Aluno', token);
  const acoes = secoesDoPapel(papelAtivo, usuarioId);

  if (carregando || !token) {
    return null;
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <TopbarAutenticada tituloDaAba="Painel" />
      <View
        className="w-full flex-1 self-center gap-five px-four py-five"
        style={{ maxWidth: MaxContentWidthPainel }}
      >
        {nome ? <Saudacao nome={nome} /> : null}
        {papelAtivo === 'Professor' && usuarioId ? (
          <ResumoValorReceberComConsulta professorId={usuarioId} />
        ) : null}
        {papelAtivo === 'Aluno' ? <ResumoDeFrequenciaDoAluno /> : null}
        {papelAtivo === 'Aluno' ? <ResumoProximoHorario proximoHorario={proximoHorario} /> : null}
        <View className="w-full flex-row flex-wrap gap-three">
          {acoes.map((acao) => (
            <CardDeAcao key={acao.label} acao={acao} />
          ))}
        </View>
      </View>
    </SafeAreaView>
  );
}

/**
 * Estado de consulta do total a receber do mês no Painel (issue #166).
 * Reaproveita `listarValorDevido(professorId)` sem período — mesma chamada
 * que o filtro "Este mês" da tela de valor devido, então o backend resolve
 * o mês corrente (contrato do filtro "Este mês"). A tela compõe o estado de
 * consulta; `ResumoValorReceber` é só apresentação (mesma separação de
 * `valor-devido.tsx`/`ValorDevidoCard`).
 */
function ResumoValorReceberComConsulta({ professorId }: { professorId: string }) {
  const [resultado, setResultado] = useState<ListarValorDevidoResultado | undefined>(undefined);

  useEffect(() => {
    let cancelado = false;
    listarValorDevido(professorId).then((dados) => {
      if (!cancelado) setResultado(dados);
    });
    return () => {
      cancelado = true;
    };
  }, [professorId]);

  if (!resultado) {
    return <ActivityIndicator accessibilityLabel="Carregando" />;
  }
  if (!resultado.sucesso) {
    return <ErrorMessage>{resultado.mensagem}</ErrorMessage>;
  }
  const total = resultado.valoresDevidos.reduce(
    (soma, item) => (item.valor === null ? soma : soma + item.valor),
    0,
  );
  return <ResumoValorReceber total={total} />;
}

function CardDeAcao({ acao }: { acao: Secao }) {
  return (
    <Link href={acao.href} asChild>
      <Pressable
        accessibilityRole="button"
        className="min-w-[160px] flex-1 items-start justify-center gap-one rounded-medium border border-background-selected bg-background-element px-four py-four active:opacity-80 dark:border-dark-background-selected dark:bg-dark-background-element"
        style={{ minHeight: 72 }}
      >
        <Text className="text-base font-semibold text-text dark:text-dark-text">{acao.label}</Text>
      </Pressable>
    </Link>
  );
}

/**
 * Resumo de frequência recente do Aluno (issue #167): consulta os últimos
 * 30 dias do mesmo `GET /alunos/historico-frequencia` da tela de histórico e
 * deriva as contagens de `Presente`/`Ausente` — sem duplicar a lógica de
 * cálculo (quem define os status é o backend, via `FrequenciaService`).
 * `NaoRegistrada`/`Cancelada` não entram no par "compareceu/faltou", então
 * ambas zero → estado vazio (mensagem clara, não seção em branco). Só monta
 * quando `papelAtivo === 'Aluno'` (o cartão não existe para o Professor).
 */
function ResumoDeFrequenciaDoAluno() {
  const { resumo, erro } = useResumoDeFrequencia();

  if (erro) {
    return null; // sem resumo em falha de rede; as ações do Painel continuam inteiras
  }
  if (resumo === undefined) {
    return (
      <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">
        Carregando…
      </Text>
    );
  }
  return <ResumoFrequenciaCard presentes={resumo.presentes} ausentes={resumo.ausentes} />;
}

/**
 * Busca o histórico dos últimos 30 dias e reduz pro resumo de
 * presenças/faltas (achado de dev-review no PR #173: extraído de
 * `ResumoDeFrequenciaDoAluno` pra caber no limite de 20 linhas por
 * função de `docs/spec/code-style.md`).
 */
function useResumoDeFrequencia(): { resumo?: { presentes: number; ausentes: number }; erro: boolean } {
  const [resumo, setResumo] = useState<{ presentes: number; ausentes: number } | undefined>(undefined);
  const [erro, setErro] = useState(false);

  useEffect(() => {
    let cancelado = false;
    listarHistoricoFrequenciaDoAluno(calcularPeriodoUltimosNDias(new Date(), 30)).then((dados) => {
      if (cancelado) return;
      if (!dados.sucesso) {
        setErro(true);
        return;
      }
      setResumo(calcularResumo(dados.historico));
      setErro(false);
    });
    return () => {
      cancelado = true;
    };
  }, []);

  return { resumo, erro };
}

/**
 * Conta `Presente` e `Ausente` no histórico agregado por Professor — nunca
 * mistura Professores, só soma o mesmo status entre eles (mesmo racional da
 * RN de valor devido, issue #13). Pura e fácil de testar isoladamente.
 */
function calcularResumo(historico: HistoricoFrequenciaPorProfessor[]): { presentes: number; ausentes: number } {
  let presentes = 0;
  let ausentes = 0;
  for (const porProfessor of historico) {
    for (const aula of porProfessor.aulas) {
      if (aula.status === 'Presente') presentes += 1;
      if (aula.status === 'Ausente') ausentes += 1;
    }
  }
  return { presentes, ausentes };
}
