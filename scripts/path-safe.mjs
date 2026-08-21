/**
 * Utilitário de segurança de path para o harness DeepSeek.
 *
 * Garante que caminhos relativos (`../`) ou absolutos não escapem do
 * diretório do repositório em read_file/write_file/list_dir.
 */
import { resolve, isAbsolute, relative } from 'node:path';

/**
 * Verifica se um caminho (relativo ou absoluto) resolve para um location
 * dentro de cwd. Retorna o caminho absoluto seguro, ou null se o path
 * tentar escapar do repo.
 *
 * @param {string} cwd - diretório raiz do repositório.
 * @param {string} caminho - caminho informado (relativo ou absoluto).
 * @returns {string|null} - caminho absoluto seguro ou null.
 */
export function caminhoDentroDoRepo(cwd, caminho) {
  const absoluto = isAbsolute(caminho) ? caminho : resolve(cwd, caminho);
  const rel = relative(cwd, absoluto);
  if (rel.startsWith('..') || isAbsolute(rel)) {
    return null;
  }
  return absoluto;
}
