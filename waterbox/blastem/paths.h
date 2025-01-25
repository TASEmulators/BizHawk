#ifndef PATHS_H_
#define PATHS_H_

void get_initial_browse_path(char **dst);
char *path_append(const char *base, const char *suffix);

#endif //PATHS_H_