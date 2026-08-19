import { fireEvent, render, screen } from '@testing-library/react-native';

import { FrequenciaAlunoToggle } from './FrequenciaAlunoToggle';

const aluno = { matriculaId: 'matricula-1', nome: 'Ana', identificador: 'ana@x.com' };

describe('FrequenciaAlunoToggle', () => {
  it('shows the aluno name and marks Presente as selected when presente is true', async () => {
    await render(<FrequenciaAlunoToggle aluno={aluno} presente={true} onChange={jest.fn()} />);

    expect(screen.getByText('Ana')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Presente', selected: true })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Ausente', selected: false })).toBeTruthy();
  });

  it('marks Ausente as selected when presente is false', async () => {
    await render(<FrequenciaAlunoToggle aluno={aluno} presente={false} onChange={jest.fn()} />);

    expect(screen.getByRole('button', { name: 'Ausente', selected: true })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Presente', selected: false })).toBeTruthy();
  });

  it('calls onChange with the matriculaId and the new value when a chip is pressed', async () => {
    const onChange = jest.fn();
    await render(<FrequenciaAlunoToggle aluno={aluno} presente={true} onChange={onChange} />);

    await fireEvent.press(screen.getByText('Ausente'));

    expect(onChange).toHaveBeenCalledWith('matricula-1', false);
  });
});
