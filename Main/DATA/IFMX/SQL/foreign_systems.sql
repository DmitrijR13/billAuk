create table "webdb".foreign_systems
(id serial not null,
nzp_role integer,
url char(250)
);

CREATE UNIQUE INDEX "webdb".ix_foreign_systems_1 ON foreign_systems(id);
CREATE UNIQUE INDEX "webdb".ix_foreign_systems_2 ON foreign_systems(nzp_role);
ALTER TABLE foreign_systems ADD CONSTRAINT PRIMARY KEY (id) CONSTRAINT "webdb".pk_foreign_systems;
