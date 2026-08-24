import { fireEvent, render, screen, waitFor, within } from '@testing-library/react-native';

import {
  alterarPrazoCancelamentoHorario,
  alterarTipoMarcacaoHorario,
  criarHorario,
  listarHorarios,
  removerHorario,
  TipoMarcacao,
} from '@/lib/api/horarios';

import HorariosProfessorScreen from './horarios';

// TopbarAutenticada (#77) monta o MenuNavegacao real, que já tem sua
// própria suíte (MenuNavegacao.test.tsx). Mockado aqui pra manter este
// arquivo focado no contrato da própria tela, sem precisar mockar
// usePathname/useIsTelaLarga/usePerfilLogado só por causa do menu.
jest.mock('@/components/organisms/TopbarAutenticada', () => {
  const { View } = jest.requireActual('react-native');
  return {
    TopbarAutenticada: ({ children }: { children?: React.ReactNode }) => <View>{children}</View>,
  };
});

jest.mock('expo-router', () => {
  const React = jest.requireActual('react');
  return {
    useLocalSearchParams: () => ({ professorId: 'professor-1' }),
    useRouter: () => ({ back: jest.fn(), replace: jest.fn(), canGoBack: () => false }),
    Link: ({ href, children }: { href: string; children: React.ReactElement }) =>
      React.cloneElement(children, { accessibilityHint: href }),
  };
});

jest.mock('@/lib/useIsTelaLarga', () => ({
  useIsTelaLarga: () => true,
}));

jest.mock('@/lib/api/horarios', () => ({
  alterarPrazoCancelamentoHorario: jest.fn(),
  alterarTipoMarcacaoHorario: jest.fn(),
  criarHorario: jest.fn(),
  listarHorarios: jest.fn(),
  removerHorario: jest.fn(),
  TipoMarcacao: { Livre: 0, Fixo: 1, Hibrido: 2 },
}));

const alterarPrazoCancelamentoHorarioMock = alterarPrazoCancelamentoHorario as jest.Mock;
const alterarTipoMarcacaoHorarioMock = alterarTipoMarcacaoHorario as jest.Mock;
const criarHorarioMock = criarHorario as jest.Mock;
const listarHorariosMock = listarHorarios as jest.Mock;
const removerHorarioMock = removerHorario as jest.Mock;

const horarioExistente = {
  id: 'h1',
  diaSemana: 2,
  horaInicio: '10:00:00',
  duracaoMinutos: 60,
  limiteAlunos: 1,
  tipoMarcacao: TipoMarcacao.Livre,
  prazoCancelamentoMinutos: 0,
};

// SeletorDeHora virou um campo de texto mascarado HH:mm (issue #138) — não
// mais um painel de listas roláveis com botão "Confirmar".
async function selecionarHora(hora: string, minuto: string) {
  await fireEvent.changeText(screen.getByLabelText('Hora de início'), `${hora}${minuto}`);
}

async function selecionarPoliticaEEnviar() {
  await fireEvent.press(screen.getAllByRole('button', { name: 'Livre' })[0]);
  await fireEvent.press(screen.getByText('Adicionar horário'));
}

// A visualização por dia (issue de usabilidade, abas 50/50) só mostra o
// horário de um dia quando a aba correspondente está selecionada — a aba
// padrão é a de hoje, que varia conforme a data real de execução do teste,
// então os testes que dependem do `horarioExistente` (Terça, diaSemana 2)
// selecionam essa aba explicitamente em vez de contar com o default.
function abasDeDiaSemana() {
  return within(screen.getByTestId('abas-dia-semana'));
}

async function selecionarAbaTerca() {
  await fireEvent.press(abasDeDiaSemana().getByRole('button', { name: 'Ter' }));
}

async function selecionarAbaQuarta() {
  await fireEvent.press(abasDeDiaSemana().getByRole('button', { name: 'Qua' }));
}

describe('HorariosProfessorScreen', () => {
  beforeEach(() => {
    alterarPrazoCancelamentoHorarioMock.mockReset();
    alterarTipoMarcacaoHorarioMock.mockReset();
    criarHorarioMock.mockReset();
    listarHorariosMock.mockReset();
    removerHorarioMock.mockReset();
    listarHorariosMock.mockResolvedValue({ sucesso: true, horarios: [horarioExistente] });
    // "Salvar" no painel de edição sempre dispara os dois handlers (política
    // + prazo, issue #187) — default de sucesso pros dois, sobrescrito nos
    // testes que verificam o comportamento específico de cada um.
    alterarTipoMarcacaoHorarioMock.mockResolvedValue({ sucesso: true, horario: horarioExistente });
    alterarPrazoCancelamentoHorarioMock.mockResolvedValue({
      sucesso: true,
      horario: horarioExistente,
    });
  });

  it('shows the horarios form directly on mount, without any gate, even without a ConfiguracaoProfessor', async () => {
    await render(<HorariosProfessorScreen />);

    expect(screen.getByText('Adicionar horário')).toBeTruthy();
    expect(screen.queryByText('Definir modelo')).toBeNull();
    await waitFor(() => expect(listarHorariosMock).toHaveBeenCalledWith('professor-1'));
  });

  it('loads and shows the existing horarios on mount, on the horario dia tab', async () => {
    await render(<HorariosProfessorScreen />);
    await waitFor(() => expect(abasDeDiaSemana().getByRole('button', { name: 'Ter' })).toBeTruthy());

    await selecionarAbaTerca();

    expect(screen.getAllByText(/Terça/)[0]).toBeTruthy();
    expect(listarHorariosMock).toHaveBeenCalledWith('professor-1');
  });

  it('highlights the tab of a day that already has a horario cadastrado', async () => {
    await render(<HorariosProfessorScreen />);

    await waitFor(() => expect(abasDeDiaSemana().getByRole('button', { name: 'Ter' })).toBeTruthy());

    const abaComHorario = abasDeDiaSemana().getByRole('button', { name: 'Ter' });
    const abaSemHorario = abasDeDiaSemana().getByRole('button', { name: 'Seg' });
    expect(abaComHorario.props.className).toContain('border-primary');
    expect(abaSemHorario.props.className).not.toContain('border-primary');
  });

  it('has a single dia-da-semana selector shared by cadastro e visualização, not one per side', async () => {
    await render(<HorariosProfessorScreen />);
    await waitFor(() => expect(abasDeDiaSemana().getByRole('button', { name: 'Ter' })).toBeTruthy());

    expect(screen.getAllByRole('button', { name: 'Ter' })).toHaveLength(1);
    expect(screen.getByText(/Novo horário para/)).toBeTruthy();
  });

  it('creates the horario for the day currently selected in the shared tab, not a fixed default', async () => {
    criarHorarioMock.mockResolvedValue({ sucesso: true, horario: horarioExistente });
    await render(<HorariosProfessorScreen />);
    await waitFor(() => expect(abasDeDiaSemana().getByRole('button', { name: 'Ter' })).toBeTruthy());

    await selecionarAbaQuarta();
    await selecionarHora('09', '00');
    await fireEvent.changeText(screen.getByPlaceholderText('60'), '30');
    await selecionarPoliticaEEnviar();

    expect(criarHorarioMock).toHaveBeenCalledWith(
      'professor-1',
      expect.objectContaining({ diaSemana: 3 }),
    );
  });

  it('adds the created horario to the list on success', async () => {
    const novoHorario = {
      id: 'h2',
      diaSemana: 3,
      horaInicio: '09:00:00',
      duracaoMinutos: 30,
      tipoMarcacao: TipoMarcacao.Livre,
    };
    criarHorarioMock.mockResolvedValue({ sucesso: true, horario: novoHorario });
    await render(<HorariosProfessorScreen />);
    await waitFor(() => expect(abasDeDiaSemana().getByRole('button', { name: 'Ter' })).toBeTruthy());

    await selecionarHora('09', '00');
    await fireEvent.changeText(screen.getByPlaceholderText('60'), '30');
    await selecionarPoliticaEEnviar();

    await selecionarAbaQuarta();
    await waitFor(() => expect(screen.getAllByText(/Quarta/)[0]).toBeTruthy());
    expect(criarHorarioMock).toHaveBeenCalledWith(
      'professor-1',
      expect.objectContaining({ tipoMarcacao: TipoMarcacao.Livre }),
    );
  });

  it('shows the Api error message when creation fails', async () => {
    criarHorarioMock.mockResolvedValue({ sucesso: false, mensagem: 'Horário conflita.' });
    await render(<HorariosProfessorScreen />);
    await waitFor(() => expect(abasDeDiaSemana().getByRole('button', { name: 'Ter' })).toBeTruthy());

    await selecionarHora('14', '00');
    await fireEvent.changeText(screen.getByPlaceholderText('60'), '30');
    await selecionarPoliticaEEnviar();

    await waitFor(() => expect(screen.getByText('Horário conflita.')).toBeTruthy());
  });

  it('removes the horario from the list when removal succeeds', async () => {
    removerHorarioMock.mockResolvedValue({ sucesso: true });
    await render(<HorariosProfessorScreen />);
    await waitFor(() => expect(abasDeDiaSemana().getByRole('button', { name: 'Ter' })).toBeTruthy());
    await selecionarAbaTerca();
    await waitFor(() => expect(screen.getAllByText(/Terça/)[0]).toBeTruthy());

    await fireEvent.press(screen.getByText('Remover'));

    await waitFor(() =>
      expect(screen.getByText('Nenhum horário cadastrado para este dia.')).toBeTruthy(),
    );
    expect(removerHorarioMock).toHaveBeenCalledWith('professor-1', 'h1');
  });

  it('shows the Api error message and keeps the horario when removal fails', async () => {
    removerHorarioMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'Não é possível remover: existem Alunos alocados.',
    });
    await render(<HorariosProfessorScreen />);
    await waitFor(() => expect(abasDeDiaSemana().getByRole('button', { name: 'Ter' })).toBeTruthy());
    await selecionarAbaTerca();
    await waitFor(() => expect(screen.getAllByText(/Terça/)[0]).toBeTruthy());

    await fireEvent.press(screen.getByText('Remover'));

    await waitFor(() =>
      expect(screen.getByText('Não é possível remover: existem Alunos alocados.')).toBeTruthy(),
    );
    expect(screen.getAllByText(/Terça/)[0]).toBeTruthy();
  });

  describe('handleAlterarPolitica (issue #71)', () => {
    it('updates the local list with the returned horario when the politica changes', async () => {
      const horarioComPoliticaAlterada = { ...horarioExistente, tipoMarcacao: TipoMarcacao.Fixo };
      alterarTipoMarcacaoHorarioMock.mockResolvedValue({
        sucesso: true,
        horario: horarioComPoliticaAlterada,
      });
      // Mesma linha no backend real: a resposta do PATCH de prazo (disparado
      // junto no mesmo "Salvar", issue #187) sempre reflete o estado cheio e
      // atual do Horario, então também carrega a política já alterada.
      alterarPrazoCancelamentoHorarioMock.mockResolvedValue({
        sucesso: true,
        horario: horarioComPoliticaAlterada,
      });
      await render(<HorariosProfessorScreen />);
      await waitFor(() => expect(abasDeDiaSemana().getByRole('button', { name: 'Ter' })).toBeTruthy());
      await selecionarAbaTerca();
      await waitFor(() => expect(screen.getAllByText(/Terça/)[0]).toBeTruthy());

      await fireEvent.press(screen.getByText('Editar política'));
      await fireEvent.press(screen.getAllByRole('button', { name: 'Fixo' })[1]);
      await fireEvent.press(screen.getByText('Salvar'));

      expect(alterarTipoMarcacaoHorarioMock).toHaveBeenCalledWith(
        'professor-1',
        'h1',
        TipoMarcacao.Fixo,
      );
      await waitFor(() =>
        expect(screen.getByTestId('horario-politica-atual')).toHaveTextContent('Fixo'),
      );
    });

    it('shows the Api error message and keeps the horario when the politica change fails', async () => {
      alterarTipoMarcacaoHorarioMock.mockResolvedValue({
        sucesso: false,
        mensagem: 'Tipo de marcação inválido.',
      });
      await render(<HorariosProfessorScreen />);
      await waitFor(() => expect(abasDeDiaSemana().getByRole('button', { name: 'Ter' })).toBeTruthy());
      await selecionarAbaTerca();
      await waitFor(() => expect(screen.getAllByText(/Terça/)[0]).toBeTruthy());

      await fireEvent.press(screen.getByText('Editar política'));
      await fireEvent.press(screen.getAllByRole('button', { name: 'Fixo' })[1]);
      await fireEvent.press(screen.getByText('Salvar'));

      await waitFor(() => expect(screen.getByText('Tipo de marcação inválido.')).toBeTruthy());
      expect(screen.getByTestId('horario-politica-atual')).toHaveTextContent('Livre');
    });
  });

  describe('handleAlterarPrazoCancelamento (issue #187)', () => {
    it('updates the local list with the returned horario when the prazo changes', async () => {
      const horarioComPrazoAlterado = { ...horarioExistente, prazoCancelamentoMinutos: 120 };
      alterarPrazoCancelamentoHorarioMock.mockResolvedValue({
        sucesso: true,
        horario: horarioComPrazoAlterado,
      });
      await render(<HorariosProfessorScreen />);
      await waitFor(() => expect(abasDeDiaSemana().getByRole('button', { name: 'Ter' })).toBeTruthy());
      await selecionarAbaTerca();
      await waitFor(() => expect(screen.getAllByText(/Terça/)[0]).toBeTruthy());

      await fireEvent.press(screen.getByText('Editar política'));
      const camposDeZero = screen.getAllByPlaceholderText('0');
      await fireEvent.changeText(camposDeZero[camposDeZero.length - 1], '120');
      await fireEvent.press(screen.getByText('Salvar'));

      expect(alterarPrazoCancelamentoHorarioMock).toHaveBeenCalledWith('professor-1', 'h1', 120);
    });

    it('shows the Api error message when the prazo change fails', async () => {
      alterarPrazoCancelamentoHorarioMock.mockResolvedValue({
        sucesso: false,
        mensagem: 'Prazo de cancelamento inválido.',
      });
      await render(<HorariosProfessorScreen />);
      await waitFor(() => expect(abasDeDiaSemana().getByRole('button', { name: 'Ter' })).toBeTruthy());
      await selecionarAbaTerca();
      await waitFor(() => expect(screen.getAllByText(/Terça/)[0]).toBeTruthy());

      await fireEvent.press(screen.getByText('Editar política'));
      const camposDeZero = screen.getAllByPlaceholderText('0');
      await fireEvent.changeText(camposDeZero[camposDeZero.length - 1], '120');
      await fireEvent.press(screen.getByText('Salvar'));

      await waitFor(() => expect(screen.getByText('Prazo de cancelamento inválido.')).toBeTruthy());
    });
  });
});
