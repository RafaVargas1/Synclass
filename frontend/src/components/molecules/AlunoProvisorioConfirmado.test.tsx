import { render, screen } from '@testing-library/react-native';

import { AlunoProvisorioConfirmado } from './AlunoProvisorioConfirmado';

describe('AlunoProvisorioConfirmado', () => {
  it('shows the Aluno name and the system-generated identifier (issue #159)', async () => {
    await render(<AlunoProvisorioConfirmado nome="João Pedro" identificador="ALU-4F2A" />);

    expect(screen.getByText('Aluno provisório cadastrado!')).toBeTruthy();
    expect(screen.getByText(/João Pedro/)).toBeTruthy();
    expect(screen.getByText('ALU-4F2A')).toBeTruthy();
  });
});
