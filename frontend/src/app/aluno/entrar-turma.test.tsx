import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { aceitarConvitePorCodigo } from '@/lib/api/convites';

import EntrarTurmaScreen from './entrar-turma';

jest.mock('@/lib/api/convites', () => ({
  aceitarConvitePorCodigo: jest.fn(),
}));

const aceitarConvitePorCodigoMock = aceitarConvitePorCodigo as jest.Mock;

describe('EntrarTurmaScreen', () => {
  beforeEach(() => {
    aceitarConvitePorCodigoMock.mockReset();
  });

  it('shows an inline confirmation when the Api responds with success', async () => {
    aceitarConvitePorCodigoMock.mockResolvedValue({
      sucesso: true,
      usuarioId: 'usuario-1',
      nome: 'João Pedro',
      papeis: ['Aluno'],
    });
    await render(<EntrarTurmaScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('12345'), '12345');
    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'João Pedro');
    await fireEvent.changeText(screen.getByPlaceholderText('E-mail ou telefone'), '11987654321');
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() => expect(screen.getByText('Cadastro concluído!')).toBeTruthy());
    expect(aceitarConvitePorCodigoMock).toHaveBeenCalledWith({
      codigo: '12345',
      nome: 'João Pedro',
      contato: '(11) 98765-4321',
    });
  });

  it('normalizes a masked code before submitting', async () => {
    aceitarConvitePorCodigoMock.mockResolvedValue({
      sucesso: true,
      usuarioId: 'usuario-1',
      nome: 'João Pedro',
      papeis: ['Aluno'],
    });
    await render(<EntrarTurmaScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('12345'), '1-2-3-4-5');
    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'João Pedro');
    await fireEvent.changeText(screen.getByPlaceholderText('E-mail ou telefone'), '11987654321');
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() => expect(aceitarConvitePorCodigoMock).toHaveBeenCalled());
    expect(aceitarConvitePorCodigoMock).toHaveBeenCalledWith(
      expect.objectContaining({ codigo: '12345' }),
    );
  });

  it('shows the Api error message without crashing when the Api rejects with a business error', async () => {
    aceitarConvitePorCodigoMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'Convite inválido ou já utilizado.',
    });
    await render(<EntrarTurmaScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('12345'), '99999');
    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'João Pedro');
    await fireEvent.changeText(screen.getByPlaceholderText('E-mail ou telefone'), '11900000000');
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() =>
      expect(screen.getByText('Convite inválido ou já utilizado.')).toBeTruthy(),
    );
    expect(screen.getByText('Cadastrar')).toBeTruthy();
  });

  it('shows a dedicated expired state with the action to ask for a new code', async () => {
    aceitarConvitePorCodigoMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'Este convite expirou. Peça ao Professor para gerar um novo link.',
    });
    await render(<EntrarTurmaScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('12345'), '12345');
    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'João Pedro');
    await fireEvent.changeText(screen.getByPlaceholderText('E-mail ou telefone'), '11987654321');
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() => expect(screen.getByText('Convite expirado')).toBeTruthy());
    expect(screen.getByText('Peça ao Professor para gerar um novo link de convite.')).toBeTruthy();
  });

  it('shows the Api error message when the contato is already linked to the Professor', async () => {
    aceitarConvitePorCodigoMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'Este Aluno já está vinculado a este Professor.',
    });
    await render(<EntrarTurmaScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('12345'), '12345');
    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'João Pedro');
    await fireEvent.changeText(screen.getByPlaceholderText('E-mail ou telefone'), '11987654321');
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() =>
      expect(screen.getByText('Este Aluno já está vinculado a este Professor.')).toBeTruthy(),
    );
  });
});
