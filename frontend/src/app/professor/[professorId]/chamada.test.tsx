import { render, screen } from '@testing-library/react-native';

import { listarHorarios } from '@/lib/api/horarios';

import ChamadaProfessorScreen from './chamada';

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
    Link: ({ href, children }: { href: string; children: React.ReactElement }) =>
      React.cloneElement(children, { accessibilityHint: href }),
  };
});

jest.mock('@/lib/api/horarios', () => ({
  listarHorarios: jest.fn(),
}));

const listarHorariosMock = listarHorarios as jest.Mock;

const horarioSegunda = {
  id: 'h1',
  diaSemana: 1,
  horaInicio: '10:00:00',
  duracaoMinutos: 60,
  limiteAlunos: 1,
  tipoMarcacao: 0,
  prazoCancelamentoMinutos: 0,
};

describe('ChamadaProfessorScreen', () => {
  beforeEach(() => {
    listarHorariosMock.mockReset();
  });

  it('mostra mensagem de vazio quando não há horários cadastrados', async () => {
    listarHorariosMock.mockResolvedValue({ sucesso: true, horarios: [] });

    await render(<ChamadaProfessorScreen />);

    expect(await screen.findByText('Nenhum horário cadastrado ainda.')).toBeTruthy();
  });

  it('lista os horários agrupados por dia, cada um com link direto pra chamada', async () => {
    listarHorariosMock.mockResolvedValue({ sucesso: true, horarios: [horarioSegunda] });

    await render(<ChamadaProfessorScreen />);

    expect(await screen.findByText('Segunda')).toBeTruthy();
    expect(screen.getByText('10:00')).toBeTruthy();
    const link = screen.getByText('Fazer chamada');
    expect(link.parent?.props.accessibilityHint).toBe(
      '/professor/professor-1/horarios/h1/chamada',
    );
  });
});
