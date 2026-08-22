import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { alocarAluno, desalocarAluno, listarAlocacoes } from '@/lib/api/alocacoes';
import { listarAlunosProvisorios } from '@/lib/api/alunosProvisorios';
import { listarHorarios, TipoMarcacao } from '@/lib/api/horarios';

import AlocacoesProfessorScreen from './alocacoes';

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

jest.mock('expo-router', () => ({
  useLocalSearchParams: () => ({ professorId: 'professor-1' }),
  useRouter: () => ({ back: jest.fn(), replace: jest.fn(), canGoBack: () => false }),
  // `Link` monta um `asChild` renderizando o filho (padrão do Painel) — aqui
  // apenas repassa os filhos pra não precisar navegar de verdade no teste.
  Link: ({ children }: { children?: React.ReactNode }) => children,
}));

jest.mock('@/lib/api/horarios', () => ({
  listarHorarios: jest.fn(),
  TipoMarcacao: { Livre: 0, Fixo: 1, Hibrido: 2 },
}));
jest.mock('@/lib/api/alunosProvisorios', () => ({ listarAlunosProvisorios: jest.fn() }));
jest.mock('@/lib/api/alocacoes', () => ({
  listarAlocacoes: jest.fn(),
  alocarAluno: jest.fn(),
  desalocarAluno: jest.fn(),
}));

const listarHorariosMock = listarHorarios as jest.Mock;
const listarAlunosProvisoriosMock = listarAlunosProvisorios as jest.Mock;
const listarAlocacoesMock = listarAlocacoes as jest.Mock;
const alocarAlunoMock = alocarAluno as jest.Mock;
const desalocarAlunoMock = desalocarAluno as jest.Mock;

const horarioFixo = {
  id: 'h1',
  diaSemana: 2,
  horaInicio: '10:00:00',
  duracaoMinutos: 60,
  limiteAlunos: 2,
  tipoMarcacao: TipoMarcacao.Fixo,
};
const horarioHibrido = { ...horarioFixo, id: 'h2', tipoMarcacao: TipoMarcacao.Hibrido };
const horarioLivre = { ...horarioFixo, id: 'h3', tipoMarcacao: TipoMarcacao.Livre };
const aluno = { matriculaId: 'a1', nome: 'Ana', identificador: 'ana@x.com' };
const alocacao = { id: 'al1', horarioId: 'h1', matriculaId: 'a1', createdAt: '2026-01-01T00:00:00Z' };

describe('AlocacoesProfessorScreen', () => {
  beforeEach(() => {
    listarHorariosMock.mockReset();
    listarAlunosProvisoriosMock.mockReset();
    listarAlocacoesMock.mockReset();
    alocarAlunoMock.mockReset();
    desalocarAlunoMock.mockReset();
    listarHorariosMock.mockResolvedValue({ sucesso: true, horarios: [horarioFixo] });
    listarAlunosProvisoriosMock.mockResolvedValue({ sucesso: true, alunos: [aluno] });
    listarAlocacoesMock.mockResolvedValue({ sucesso: true, alocacoes: [] });
  });

  it('shows a loading indicator while listarHorarios is pending', async () => {
    listarHorariosMock.mockReturnValue(new Promise(() => {}));

    await render(<AlocacoesProfessorScreen />);

    expect(screen.getByLabelText('Carregando')).toBeTruthy();
  });

  it('shows an error message with a retry action when listarHorarios fails', async () => {
    listarHorariosMock.mockResolvedValue({ sucesso: false, mensagem: 'Erro de conexão.' });

    await render(<AlocacoesProfessorScreen />);

    await waitFor(() => expect(screen.getByText('Erro de conexão.')).toBeTruthy());
    expect(screen.getByText('Tentar novamente')).toBeTruthy();
  });

  it('shows a message with a CTA instead of the grid when there are no horarios', async () => {
    listarHorariosMock.mockResolvedValue({ sucesso: true, horarios: [] });

    await render(<AlocacoesProfessorScreen />);

    await waitFor(() =>
      expect(screen.getByText('Nenhum horário cadastrado ainda.')).toBeTruthy(),
    );
    expect(screen.getByText('Cadastrar horários')).toBeTruthy();
  });

  it('explains that no horario accepts allocation when all of them are Livre (issue #157)', async () => {
    listarHorariosMock.mockResolvedValue({ sucesso: true, horarios: [horarioLivre] });

    await render(<AlocacoesProfessorScreen />);

    await waitFor(() =>
      expect(
        screen.getByText(
          'Nenhum dos seus horários aceita atribuição manual — todos são do tipo Livre, os Alunos se inscrevem sozinhos.',
        ),
      ).toBeTruthy(),
    );
  });

  it('renders the alocacao grid only for Fixo/Hibrido horarios, filtering out Livre ones (issue #157)', async () => {
    listarHorariosMock.mockResolvedValue({
      sucesso: true,
      horarios: [horarioFixo, horarioHibrido, horarioLivre],
    });

    await render(<AlocacoesProfessorScreen />);

    await waitFor(() => expect(screen.getAllByText('Ana').length).toBe(2));
    expect(screen.queryByText('Nenhum horário cadastrado ainda.')).toBeNull();
    expect(
      screen.queryByText(
        'Nenhum dos seus horários aceita atribuição manual — todos são do tipo Livre, os Alunos se inscrevem sozinhos.',
      ),
    ).toBeNull();
  });

  it('allocates the selected aluno and updates the card on success', async () => {
    alocarAlunoMock.mockResolvedValue({ sucesso: true, alocacao });
    await render(<AlocacoesProfessorScreen />);
    await waitFor(() => expect(screen.getByText('Ana')).toBeTruthy());

    await fireEvent.press(screen.getByText('Alocar'));

    expect(alocarAlunoMock).toHaveBeenCalledWith('h1', 'a1');
    await waitFor(() => expect(screen.getByText('1/2')).toBeTruthy());
  });

  it('deallocates the aluno and updates the card on success', async () => {
    listarAlocacoesMock.mockResolvedValue({ sucesso: true, alocacoes: [alocacao] });
    desalocarAlunoMock.mockResolvedValue({ sucesso: true });
    await render(<AlocacoesProfessorScreen />);
    await waitFor(() => expect(screen.getByText('1/2')).toBeTruthy());

    await fireEvent.press(screen.getByText('Remover'));

    expect(desalocarAlunoMock).toHaveBeenCalledWith('h1', 'a1');
    await waitFor(() => expect(screen.getByText('0/2')).toBeTruthy());
  });
});
