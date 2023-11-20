let
	only-2_6_2-through-2_9 = [
		{ pname = "Cyotek.Drawing.BitmapFont"; version = "2.0.2"; sha256 = "0h1s1h7bs3iaxmyv1gi5r0712kf40njljdcff52i87fi9jz76lj9"; }
	];
	only-2_6_3_and-2_7 = [
		{ pname = "Microsoft.CodeCoverage"; version = "16.10.0"; sha256 = "0nimrz7araj72d4ckpkqprvg6rgdv5y1bvmv63pqqbphbif9fjd1"; }
		{ pname = "Microsoft.NET.Test.Sdk"; version = "16.10.0"; sha256 = "1rz5d3h2427xp86qm8l35bs7xkl93771gk8401bsd7mrrad86nwb"; }
		{ pname = "Microsoft.NETCore.Platforms"; version = "3.1.1"; sha256 = "05hmaygd5131rnqi6ipv7agsbpi7ka18779vw45iw6b385l7n987"; }
		{ pname = "Microsoft.TestPlatform.ObjectModel"; version = "16.10.0"; sha256 = "062llwghbmf5vhrczylyzhlflhd5lx05607aipmximd66p50wzbk"; }
		{ pname = "Microsoft.TestPlatform.TestHost"; version = "16.10.0"; sha256 = "1lhwwxlylmklp8wwirfmdc4dzfp2h8fxgw847cqy8by8h5wcwbjc"; }
		{ pname = "MSTest.TestAdapter"; version = "2.2.3"; sha256 = "14lfcagmcqv2kqyaanxdrhh57v6g7h790v6k9sicdbmahwnkkia2"; }
		{ pname = "MSTest.TestFramework"; version = "2.2.3"; sha256 = "1ycmya48212j4295qwldyzpm20001zdlmzna0zi9m3jvhlvhjggg"; }
		{ pname = "SharpCompress"; version = "0.26.0"; sha256 = "03cygf8p44j1bfn6z9cn2xrw6zhvhq17xac1sph5rgq7vq2m5iq5"; }
		{ pname = "System.Runtime.CompilerServices.Unsafe"; version = "4.7.1"; sha256 = "119br3pd85lq8zcgh4f60jzmv1g976q1kdgi3hvqdlhfbw6siz2j"; }
		{ pname = "System.Text.Encoding.CodePages"; version = "4.7.1"; sha256 = "1y1hdap9qbl7vp74j8s9zcbh3v1rnrrvcc55wj1hl6has2v3qh1r"; }
	];
	only-2_6_3-through-2_8 = [
		{ pname = "OpenTK"; version = "3.3.2"; sha256 = "0jcy4ldq5qp6s1441l12dycfgq0llx4i97y2bbv3n99hg7qvk9vm"; }
		{ pname = "System.Drawing.Common"; version = "5.0.2"; sha256 = "08kgiywg5whhw80xshlrp0q9mkl8hlkgqdsnk1gm6bb898f1l3gs"; }
	];
	only-2_6_3-through-2_9 = [
		{ pname = "JunitXml.TestLogger"; version = "3.0.98"; sha256 = "1xl6kjaa8b84y8fy0q60mym28nqza3vm3pd4fc8ragcmfh2k5rxv"; }
		{ pname = "Microsoft.NETFramework.ReferenceAssemblies"; version = "1.0.2"; sha256 = "0i42rn8xmvhn08799manpym06kpw89qy9080myyy2ngy565pqh0a"; }
		{ pname = "Microsoft.NETFramework.ReferenceAssemblies.net48"; version = "1.0.2"; sha256 = "10ry214g6v4s57vmk1pyqz8z3fsqxl44qyn68y0rrfjcpn29sh5m"; }
		{ pname = "System.Resources.Extensions"; version = "5.0.0"; sha256 = "0v2zdwpnqky43zhjrd2dwbap02n4pfwrhl9sz70zp3mpx7z6qxnj"; }
	];
	only-2_8-and-2_9 = [
		{ pname = "Microsoft.CodeCoverage"; version = "17.0.0"; sha256 = "18gdbsqf6i79ld4ikqr4jhx9ndsggm865b5xj1xmnmgg12ydp19a"; }
		{ pname = "Microsoft.NET.Test.Sdk"; version = "17.0.0"; sha256 = "0bknyf5kig5icwjxls7pcn51x2b2qf91dz9qv67fl70v6cczaz2r"; }
		{ pname = "Microsoft.TestPlatform.ObjectModel"; version = "17.0.0"; sha256 = "1bh5scbvl6ndldqv20sl34h4y257irm9ziv2wyfc3hka6912fhn7"; }
		{ pname = "Microsoft.TestPlatform.TestHost"; version = "17.0.0"; sha256 = "06mn31cgpp7d8lwdyjanh89prc66j37dchn74vrd9s588rq0y70r"; }
		{ pname = "MSTest.TestAdapter"; version = "2.2.8"; sha256 = "045k737srwvm6dpad04dkj7ln38csf45lps5j88w8hyzrdgllbb9"; }
		{ pname = "MSTest.TestFramework"; version = "2.2.8"; sha256 = "1jw7sl2li7xzx8scciz9bc2rw8cnwcwxr8061zykrgg1dbjx6aa3"; }
		{ pname = "SharpCompress"; version = "0.30.1"; sha256 = "1hib2hxjrlikwsczym1qn2slaapgjw8qzd8gmid8bryaz8hv044h"; }
		{ pname = "System.Collections.Immutable"; version = "5.0.0"; sha256 = "1kvcllagxz2q92g81zkz81djkn2lid25ayjfgjalncyc68i15p0r"; }
	];
in {
	until-2_6 = [];
	only-2_6 = [];
	since-2_6 = [
		{ pname = "Microsoft.CSharp"; version = "4.7.0"; sha256 = "0gd67zlw554j098kabg887b5a6pq9kzavpa3jjy5w53ccjzjfy8j"; }
		{ pname = "Microsoft.NETCore.Platforms"; version = "1.1.0"; sha256 = "08vh1r12g6ykjygq5d3vq09zylgb84l63k49jc4v8faw9g93iqqm"; }
		{ pname = "Microsoft.NETCore.Targets"; version = "1.1.0"; sha256 = "193xwf33fbm0ni3idxzbr5fdq3i2dlfgihsac9jj7whj0gd902nh"; }
		{ pname = "Microsoft.Win32.Primitives"; version = "4.3.0"; sha256 = "0j0c1wj4ndj21zsgivsc24whiya605603kxrbiw6wkfdync464wq"; }
		{ pname = "NETStandard.Library"; version = "1.6.1"; sha256 = "1z70wvsx2d847a2cjfii7b83pjfs34q05gb037fdjikv5kbagml8"; }
		{ pname = "NETStandard.Library"; version = "2.0.3"; sha256 = "1fn9fxppfcg4jgypp2pmrpr6awl3qz1xmnri0cygpkwvyx27df1y"; }
		{ pname = "runtime.any.System.Collections"; version = "4.3.0"; sha256 = "0bv5qgm6vr47ynxqbnkc7i797fdi8gbjjxii173syrx14nmrkwg0"; }
		{ pname = "runtime.any.System.Diagnostics.Tools"; version = "4.3.0"; sha256 = "1wl76vk12zhdh66vmagni66h5xbhgqq7zkdpgw21jhxhvlbcl8pk"; }
		{ pname = "runtime.any.System.Diagnostics.Tracing"; version = "4.3.0"; sha256 = "00j6nv2xgmd3bi347k00m7wr542wjlig53rmj28pmw7ddcn97jbn"; }
		{ pname = "runtime.any.System.Globalization"; version = "4.3.0"; sha256 = "1daqf33hssad94lamzg01y49xwndy2q97i2lrb7mgn28656qia1x"; }
		{ pname = "runtime.any.System.Globalization.Calendars"; version = "4.3.0"; sha256 = "1ghhhk5psqxcg6w88sxkqrc35bxcz27zbqm2y5p5298pv3v7g201"; }
		{ pname = "runtime.any.System.IO"; version = "4.3.0"; sha256 = "0l8xz8zn46w4d10bcn3l4yyn4vhb3lrj2zw8llvz7jk14k4zps5x"; }
		{ pname = "runtime.any.System.Reflection"; version = "4.3.0"; sha256 = "02c9h3y35pylc0zfq3wcsvc5nqci95nrkq0mszifc0sjx7xrzkly"; }
		{ pname = "runtime.any.System.Reflection.Extensions"; version = "4.3.0"; sha256 = "0zyri97dfc5vyaz9ba65hjj1zbcrzaffhsdlpxc9bh09wy22fq33"; }
		{ pname = "runtime.any.System.Reflection.Primitives"; version = "4.3.0"; sha256 = "0x1mm8c6iy8rlxm8w9vqw7gb7s1ljadrn049fmf70cyh42vdfhrf"; }
		{ pname = "runtime.any.System.Resources.ResourceManager"; version = "4.3.0"; sha256 = "03kickal0iiby82wa5flar18kyv82s9s6d4xhk5h4bi5kfcyfjzl"; }
		{ pname = "runtime.any.System.Runtime"; version = "4.3.0"; sha256 = "1cqh1sv3h5j7ixyb7axxbdkqx6cxy00p4np4j91kpm492rf4s25b"; }
		{ pname = "runtime.any.System.Runtime.Handles"; version = "4.3.0"; sha256 = "0bh5bi25nk9w9xi8z23ws45q5yia6k7dg3i4axhfqlnj145l011x"; }
		{ pname = "runtime.any.System.Runtime.InteropServices"; version = "4.3.0"; sha256 = "0c3g3g3jmhlhw4klrc86ka9fjbl7i59ds1fadsb2l8nqf8z3kb19"; }
		{ pname = "runtime.any.System.Text.Encoding"; version = "4.3.0"; sha256 = "0aqqi1v4wx51h51mk956y783wzags13wa7mgqyclacmsmpv02ps3"; }
		{ pname = "runtime.any.System.Text.Encoding.Extensions"; version = "4.3.0"; sha256 = "0lqhgqi0i8194ryqq6v2gqx0fb86db2gqknbm0aq31wb378j7ip8"; }
		{ pname = "runtime.any.System.Threading.Tasks"; version = "4.3.0"; sha256 = "03mnvkhskbzxddz4hm113zsch1jyzh2cs450dk3rgfjp8crlw1va"; }
		{ pname = "runtime.any.System.Threading.Timer"; version = "4.3.0"; sha256 = "0aw4phrhwqz9m61r79vyfl5la64bjxj8l34qnrcwb28v49fg2086"; }
		{ pname = "runtime.debian.8-x64.runtime.native.System.Security.Cryptography.OpenSsl"; version = "4.3.0"; sha256 = "16rnxzpk5dpbbl1x354yrlsbvwylrq456xzpsha1n9y3glnhyx9d"; }
		{ pname = "runtime.fedora.23-x64.runtime.native.System.Security.Cryptography.OpenSsl"; version = "4.3.0"; sha256 = "0hkg03sgm2wyq8nqk6dbm9jh5vcq57ry42lkqdmfklrw89lsmr59"; }
		{ pname = "runtime.fedora.24-x64.runtime.native.System.Security.Cryptography.OpenSsl"; version = "4.3.0"; sha256 = "0c2p354hjx58xhhz7wv6div8xpi90sc6ibdm40qin21bvi7ymcaa"; }
		{ pname = "runtime.native.System"; version = "4.3.0"; sha256 = "15hgf6zaq9b8br2wi1i3x0zvmk410nlmsmva9p0bbg73v6hml5k4"; }
		{ pname = "runtime.native.System.IO.Compression"; version = "4.3.0"; sha256 = "1vvivbqsk6y4hzcid27pqpm5bsi6sc50hvqwbcx8aap5ifrxfs8d"; }
		{ pname = "runtime.native.System.Net.Http"; version = "4.3.0"; sha256 = "1n6rgz5132lcibbch1qlf0g9jk60r0kqv087hxc0lisy50zpm7kk"; }
		{ pname = "runtime.native.System.Security.Cryptography.Apple"; version = "4.3.0"; sha256 = "1b61p6gw1m02cc1ry996fl49liiwky6181dzr873g9ds92zl326q"; }
		{ pname = "runtime.native.System.Security.Cryptography.OpenSsl"; version = "4.3.0"; sha256 = "18pzfdlwsg2nb1jjjjzyb5qlgy6xjxzmhnfaijq5s2jw3cm3ab97"; }
		{ pname = "runtime.opensuse.13.2-x64.runtime.native.System.Security.Cryptography.OpenSsl"; version = "4.3.0"; sha256 = "0qyynf9nz5i7pc26cwhgi8j62ps27sqmf78ijcfgzab50z9g8ay3"; }
		{ pname = "runtime.opensuse.42.1-x64.runtime.native.System.Security.Cryptography.OpenSsl"; version = "4.3.0"; sha256 = "1klrs545awhayryma6l7g2pvnp9xy4z0r1i40r80zb45q3i9nbyf"; }
		{ pname = "runtime.osx.10.10-x64.runtime.native.System.Security.Cryptography.Apple"; version = "4.3.0"; sha256 = "10yc8jdrwgcl44b4g93f1ds76b176bajd3zqi2faf5rvh1vy9smi"; }
		{ pname = "runtime.osx.10.10-x64.runtime.native.System.Security.Cryptography.OpenSsl"; version = "4.3.0"; sha256 = "0zcxjv5pckplvkg0r6mw3asggm7aqzbdjimhvsasb0cgm59x09l3"; }
		{ pname = "runtime.rhel.7-x64.runtime.native.System.Security.Cryptography.OpenSsl"; version = "4.3.0"; sha256 = "0vhynn79ih7hw7cwjazn87rm9z9fj0rvxgzlab36jybgcpcgphsn"; }
		{ pname = "runtime.ubuntu.14.04-x64.runtime.native.System.Security.Cryptography.OpenSsl"; version = "4.3.0"; sha256 = "160p68l2c7cqmyqjwxydcvgw7lvl1cr0znkw8fp24d1by9mqc8p3"; }
		{ pname = "runtime.ubuntu.16.04-x64.runtime.native.System.Security.Cryptography.OpenSsl"; version = "4.3.0"; sha256 = "15zrc8fgd8zx28hdghcj5f5i34wf3l6bq5177075m2bc2j34jrqy"; }
		{ pname = "runtime.ubuntu.16.10-x64.runtime.native.System.Security.Cryptography.OpenSsl"; version = "4.3.0"; sha256 = "1p4dgxax6p7rlgj4q73k73rslcnz4wdcv8q2flg1s8ygwcm58ld5"; }
		{ pname = "runtime.unix.Microsoft.Win32.Primitives"; version = "4.3.0"; sha256 = "0y61k9zbxhdi0glg154v30kkq7f8646nif8lnnxbvkjpakggd5id"; }
		{ pname = "runtime.unix.System.Console"; version = "4.3.0"; sha256 = "1pfpkvc6x2if8zbdzg9rnc5fx51yllprl8zkm5npni2k50lisy80"; }
		{ pname = "runtime.unix.System.Diagnostics.Debug"; version = "4.3.0"; sha256 = "1lps7fbnw34bnh3lm31gs5c0g0dh7548wfmb8zz62v0zqz71msj5"; }
		{ pname = "runtime.unix.System.IO.FileSystem"; version = "4.3.0"; sha256 = "14nbkhvs7sji5r1saj2x8daz82rnf9kx28d3v2qss34qbr32dzix"; }
		{ pname = "runtime.unix.System.Net.Primitives"; version = "4.3.0"; sha256 = "0bdnglg59pzx9394sy4ic66kmxhqp8q8bvmykdxcbs5mm0ipwwm4"; }
		{ pname = "runtime.unix.System.Net.Sockets"; version = "4.3.0"; sha256 = "03npdxzy8gfv035bv1b9rz7c7hv0rxl5904wjz51if491mw0xy12"; }
		{ pname = "runtime.unix.System.Private.Uri"; version = "4.3.0"; sha256 = "1jx02q6kiwlvfksq1q9qr17fj78y5v6mwsszav4qcz9z25d5g6vk"; }
		{ pname = "runtime.unix.System.Runtime.Extensions"; version = "4.3.0"; sha256 = "0pnxxmm8whx38dp6yvwgmh22smknxmqs5n513fc7m4wxvs1bvi4p"; }
		{ pname = "System.AppContext"; version = "4.3.0"; sha256 = "1649qvy3dar900z3g817h17nl8jp4ka5vcfmsr05kh0fshn7j3ya"; }
		{ pname = "System.Buffers"; version = "4.3.0"; sha256 = "0fgns20ispwrfqll4q1zc1waqcmylb3zc50ys9x8zlwxh9pmd9jy"; }
		{ pname = "System.Buffers"; version = "4.5.1"; sha256 = "04kb1mdrlcixj9zh1xdi5as0k0qi8byr5mi3p3jcxx72qz93s2y3"; }
		{ pname = "System.Collections"; version = "4.3.0"; sha256 = "19r4y64dqyrq6k4706dnyhhw7fs24kpp3awak7whzss39dakpxk9"; }
		{ pname = "System.Collections.Concurrent"; version = "4.3.0"; sha256 = "0wi10md9aq33jrkh2c24wr2n9hrpyamsdhsxdcnf43b7y86kkii8"; }
		{ pname = "System.ComponentModel.Annotations"; version = "5.0.0"; sha256 = "021h7x98lblq9avm1bgpa4i31c2kgsa7zn4sqhxf39g087ar756j"; }
		{ pname = "System.Console"; version = "4.3.0"; sha256 = "1flr7a9x920mr5cjsqmsy9wgnv3lvd0h1g521pdr1lkb2qycy7ay"; }
		{ pname = "System.Diagnostics.Debug"; version = "4.3.0"; sha256 = "00yjlf19wjydyr6cfviaph3vsjzg3d5nvnya26i2fvfg53sknh3y"; }
		{ pname = "System.Diagnostics.DiagnosticSource"; version = "4.3.0"; sha256 = "0z6m3pbiy0qw6rn3n209rrzf9x1k4002zh90vwcrsym09ipm2liq"; }
		{ pname = "System.Diagnostics.TextWriterTraceListener"; version = "4.3.0"; sha256 = "09db74f36wkwg30f7v7zhz1yhkyrnl5v6bdwljq1jdfgzcfch7c3"; }
		{ pname = "System.Diagnostics.Tools"; version = "4.3.0"; sha256 = "0in3pic3s2ddyibi8cvgl102zmvp9r9mchh82ns9f0ms4basylw1"; }
		{ pname = "System.Diagnostics.TraceSource"; version = "4.3.0"; sha256 = "1kyw4d7dpjczhw6634nrmg7yyyzq72k75x38y0l0nwhigdlp1766"; }
		{ pname = "System.Diagnostics.Tracing"; version = "4.3.0"; sha256 = "1m3bx6c2s958qligl67q7grkwfz3w53hpy7nc97mh6f7j5k168c4"; }
		{ pname = "System.Globalization"; version = "4.3.0"; sha256 = "1cp68vv683n6ic2zqh2s1fn4c2sd87g5hpp6l4d4nj4536jz98ki"; }
		{ pname = "System.Globalization.Calendars"; version = "4.3.0"; sha256 = "1xwl230bkakzzkrggy1l1lxmm3xlhk4bq2pkv790j5lm8g887lxq"; }
		{ pname = "System.Globalization.Extensions"; version = "4.3.0"; sha256 = "02a5zfxavhv3jd437bsncbhd2fp1zv4gxzakp1an9l6kdq1mcqls"; }
		{ pname = "System.IO"; version = "4.3.0"; sha256 = "05l9qdrzhm4s5dixmx68kxwif4l99ll5gqmh7rqgw554fx0agv5f"; }
		{ pname = "System.IO.Compression"; version = "4.3.0"; sha256 = "084zc82yi6yllgda0zkgl2ys48sypiswbiwrv7irb3r0ai1fp4vz"; }
		{ pname = "System.IO.Compression.ZipFile"; version = "4.3.0"; sha256 = "1yxy5pq4dnsm9hlkg9ysh5f6bf3fahqqb6p8668ndy5c0lk7w2ar"; }
		{ pname = "System.IO.FileSystem"; version = "4.3.0"; sha256 = "0z2dfrbra9i6y16mm9v1v6k47f0fm617vlb7s5iybjjsz6g1ilmw"; }
		{ pname = "System.IO.FileSystem.Primitives"; version = "4.3.0"; sha256 = "0j6ndgglcf4brg2lz4wzsh1av1gh8xrzdsn9f0yznskhqn1xzj9c"; }
		{ pname = "System.Linq"; version = "4.3.0"; sha256 = "1w0gmba695rbr80l1k2h4mrwzbzsyfl2z4klmpbsvsg5pm4a56s7"; }
		{ pname = "System.Linq.Expressions"; version = "4.3.0"; sha256 = "0ky2nrcvh70rqq88m9a5yqabsl4fyd17bpr63iy2mbivjs2nyypv"; }
		{ pname = "System.Memory"; version = "4.5.4"; sha256 = "14gbbs22mcxwggn0fcfs1b062521azb9fbb7c113x0mq6dzq9h6y"; }
		{ pname = "System.Net.Http"; version = "4.3.0"; sha256 = "1i4gc757xqrzflbk7kc5ksn20kwwfjhw9w7pgdkn19y3cgnl302j"; }
		{ pname = "System.Net.Http"; version = "4.3.4"; sha256 = "0kdp31b8819v88l719j6my0yas6myv9d1viql3qz5577mv819jhl"; }
		{ pname = "System.Net.NameResolution"; version = "4.3.0"; sha256 = "15r75pwc0rm3vvwsn8rvm2krf929mjfwliv0mpicjnii24470rkq"; }
		{ pname = "System.Net.Primitives"; version = "4.3.0"; sha256 = "0c87k50rmdgmxx7df2khd9qj7q35j9rzdmm2572cc55dygmdk3ii"; }
		{ pname = "System.Net.Sockets"; version = "4.3.0"; sha256 = "1ssa65k6chcgi6mfmzrznvqaxk8jp0gvl77xhf1hbzakjnpxspla"; }
		{ pname = "System.Numerics.Vectors"; version = "4.4.0"; sha256 = "0rdvma399070b0i46c4qq1h2yvjj3k013sqzkilz4bz5cwmx1rba"; }
		{ pname = "System.Numerics.Vectors"; version = "4.5.0"; sha256 = "1kzrj37yzawf1b19jq0253rcs8hsq1l2q8g69d7ipnhzb0h97m59"; }
		{ pname = "System.ObjectModel"; version = "4.3.0"; sha256 = "191p63zy5rpqx7dnrb3h7prvgixmk168fhvvkkvhlazncf8r3nc2"; }
		{ pname = "System.Private.Uri"; version = "4.3.0"; sha256 = "04r1lkdnsznin0fj4ya1zikxiqr0h6r6a1ww2dsm60gqhdrf0mvx"; }
		{ pname = "System.Reflection"; version = "4.3.0"; sha256 = "0xl55k0mw8cd8ra6dxzh974nxif58s3k1rjv1vbd7gjbjr39j11m"; }
		{ pname = "System.Reflection.Emit"; version = "4.3.0"; sha256 = "11f8y3qfysfcrscjpjym9msk7lsfxkk4fmz9qq95kn3jd0769f74"; }
		{ pname = "System.Reflection.Emit.ILGeneration"; version = "4.3.0"; sha256 = "0w1n67glpv8241vnpz1kl14sy7zlnw414aqwj4hcx5nd86f6994q"; }
		{ pname = "System.Reflection.Emit.ILGeneration"; version = "4.7.0"; sha256 = "0l8jpxhpgjlf1nkz5lvp61r4kfdbhr29qi8aapcxn3izd9wd0j8r"; }
		{ pname = "System.Reflection.Emit.Lightweight"; version = "4.3.0"; sha256 = "0ql7lcakycrvzgi9kxz1b3lljd990az1x6c4jsiwcacrvimpib5c"; }
		{ pname = "System.Reflection.Emit.Lightweight"; version = "4.7.0"; sha256 = "0mbjfajmafkca47zr8v36brvknzks5a7pgb49kfq2d188pyv6iap"; }
		{ pname = "System.Reflection.Extensions"; version = "4.3.0"; sha256 = "02bly8bdc98gs22lqsfx9xicblszr2yan7v2mmw3g7hy6miq5hwq"; }
		{ pname = "System.Reflection.Metadata"; version = "1.6.0"; sha256 = "1wdbavrrkajy7qbdblpbpbalbdl48q3h34cchz24gvdgyrlf15r4"; }
		{ pname = "System.Reflection.Primitives"; version = "4.3.0"; sha256 = "04xqa33bld78yv5r93a8n76shvc8wwcdgr1qvvjh959g3rc31276"; }
		{ pname = "System.Reflection.TypeExtensions"; version = "4.3.0"; sha256 = "0y2ssg08d817p0vdag98vn238gyrrynjdj4181hdg780sif3ykp1"; }
		{ pname = "System.Resources.ResourceManager"; version = "4.3.0"; sha256 = "0sjqlzsryb0mg4y4xzf35xi523s4is4hz9q4qgdvlvgivl7qxn49"; }
		{ pname = "System.Runtime"; version = "4.3.0"; sha256 = "066ixvgbf2c929kgknshcxqj6539ax7b9m570cp8n179cpfkapz7"; }
		{ pname = "System.Runtime.CompilerServices.Unsafe"; version = "4.5.3"; sha256 = "1afi6s2r1mh1kygbjmfba6l4f87pi5sg13p4a48idqafli94qxln"; }
		{ pname = "System.Runtime.Extensions"; version = "4.3.0"; sha256 = "1ykp3dnhwvm48nap8q23893hagf665k0kn3cbgsqpwzbijdcgc60"; }
		{ pname = "System.Runtime.Handles"; version = "4.3.0"; sha256 = "0sw2gfj2xr7sw9qjn0j3l9yw07x73lcs97p8xfc9w1x9h5g5m7i8"; }
		{ pname = "System.Runtime.InteropServices"; version = "4.3.0"; sha256 = "00hywrn4g7hva1b2qri2s6rabzwgxnbpw9zfxmz28z09cpwwgh7j"; }
		{ pname = "System.Runtime.InteropServices.RuntimeInformation"; version = "4.3.0"; sha256 = "0q18r1sh4vn7bvqgd6dmqlw5v28flbpj349mkdish2vjyvmnb2ii"; }
		{ pname = "System.Runtime.Numerics"; version = "4.3.0"; sha256 = "19rav39sr5dky7afygh309qamqqmi9kcwvz3i0c5700v0c5cg61z"; }
		{ pname = "System.Security.Claims"; version = "4.3.0"; sha256 = "0jvfn7j22l3mm28qjy3rcw287y9h65ha4m940waaxah07jnbzrhn"; }
		{ pname = "System.Security.Cryptography.Algorithms"; version = "4.3.0"; sha256 = "03sq183pfl5kp7gkvq77myv7kbpdnq3y0xj7vi4q1kaw54sny0ml"; }
		{ pname = "System.Security.Cryptography.Cng"; version = "4.3.0"; sha256 = "1k468aswafdgf56ab6yrn7649kfqx2wm9aslywjam1hdmk5yypmv"; }
		{ pname = "System.Security.Cryptography.Csp"; version = "4.3.0"; sha256 = "1x5wcrddf2s3hb8j78cry7yalca4lb5vfnkrysagbn6r9x6xvrx1"; }
		{ pname = "System.Security.Cryptography.Encoding"; version = "4.3.0"; sha256 = "1jr6w70igqn07k5zs1ph6xja97hxnb3mqbspdrff6cvssgrixs32"; }
		{ pname = "System.Security.Cryptography.OpenSsl"; version = "4.3.0"; sha256 = "0givpvvj8yc7gv4lhb6s1prq6p2c4147204a0wib89inqzd87gqc"; }
		{ pname = "System.Security.Cryptography.Primitives"; version = "4.3.0"; sha256 = "0pyzncsv48zwly3lw4f2dayqswcfvdwq2nz0dgwmi7fj3pn64wby"; }
		{ pname = "System.Security.Cryptography.X509Certificates"; version = "4.3.0"; sha256 = "0valjcz5wksbvijylxijjxb1mp38mdhv03r533vnx1q3ikzdav9h"; }
		{ pname = "System.Security.Principal"; version = "4.3.0"; sha256 = "12cm2zws06z4lfc4dn31iqv7072zyi4m910d4r6wm8yx85arsfxf"; }
		{ pname = "System.Security.Principal.Windows"; version = "4.3.0"; sha256 = "00a0a7c40i3v4cb20s2cmh9csb5jv2l0frvnlzyfxh848xalpdwr"; }
		{ pname = "System.Text.Encoding"; version = "4.3.0"; sha256 = "1f04lkir4iladpp51sdgmis9dj4y8v08cka0mbmsy0frc9a4gjqr"; }
		{ pname = "System.Text.Encoding.Extensions"; version = "4.3.0"; sha256 = "11q1y8hh5hrp5a3kw25cb6l00v5l5dvirkz8jr3sq00h1xgcgrxy"; }
		{ pname = "System.Text.RegularExpressions"; version = "4.3.0"; sha256 = "1bgq51k7fwld0njylfn7qc5fmwrk2137gdq7djqdsw347paa9c2l"; }
		{ pname = "System.Threading"; version = "4.3.0"; sha256 = "0rw9wfamvhayp5zh3j7p1yfmx9b5khbf4q50d8k5rk993rskfd34"; }
		{ pname = "System.Threading.Tasks"; version = "4.3.0"; sha256 = "134z3v9abw3a6jsw17xl3f6hqjpak5l682k2vz39spj4kmydg6k7"; }
		{ pname = "System.Threading.Tasks.Extensions"; version = "4.3.0"; sha256 = "1xxcx2xh8jin360yjwm4x4cf5y3a2bwpn2ygkfkwkicz7zk50s2z"; }
		{ pname = "System.Threading.ThreadPool"; version = "4.3.0"; sha256 = "027s1f4sbx0y1xqw2irqn6x161lzj8qwvnh2gn78ciiczdv10vf1"; }
		{ pname = "System.Threading.Timer"; version = "4.3.0"; sha256 = "1nx773nsx6z5whv8kaa1wjh037id2f1cxhb69pvgv12hd2b6qs56"; }
		{ pname = "System.Xml.ReaderWriter"; version = "4.3.0"; sha256 = "0c47yllxifzmh8gq6rq6l36zzvw4kjvlszkqa9wq3fr59n0hl3s1"; }
		{ pname = "System.Xml.XDocument"; version = "4.3.0"; sha256 = "08h8fm4l77n0nd4i4fk2386y809bfbwqb7ih9d7564ifcxr5ssxd"; }
	];

	until-2_6_1 = [
		{ pname = "Microsoft.VisualBasic"; version = "10.3.0"; sha256 = "1l2351srrbxjvf5kdzid50246fapb957xxiqi8n0caf5wbs8lwzq"; }
		{ pname = "SharpZipLib"; version = "1.1.0"; sha256 = "1jlqnza79sicf5p6v4ssyn70j7gbd3yrcjhgv5iykzb7d9kbcjqg"; }
	];
	only-2_6_1 = [];
	since-2_6_1 = [];

	until-2_6_2 = [
		{ pname = "Cyotek.Drawing.BitmapFont"; version = "1.0.2"; sha256 = "18rzr6b2403i8i51h18zf82lwkdvnyw9bn4qd7l7sk1xlc9qgjga"; }
		{ pname = "OpenTK"; version = "3.1.0"; sha256 = "0qh541p4mlih5i1bjla8yzv21dv7gpmqijf9k0r6wjc309pcf9pp"; }
	];
	only-2_6_2 = only-2_6_2-through-2_9;
	since-2_6_2 = [];

	until-2_6_3 = [
		{ pname = "JunitXml.TestLogger"; version = "2.1.78"; sha256 = "0sdh4hckig6jbr8hbmih1lc8ax1lcvari0dfy9y5ipvvphdkcn5k"; }
		{ pname = "Microsoft.CodeCoverage"; version = "16.8.3"; sha256 = "1r49rr83s9agbs3xkmf1dlkvqbay163qvwz6r2z6lgh92r9flksv"; }
		{ pname = "Microsoft.NET.Test.Sdk"; version = "16.8.3"; sha256 = "1ahah4vgdjfymab7cd49rpdagsjd1x1hid3h82yln99b3p91whkd"; }
		{ pname = "Microsoft.NETCore.Platforms"; version = "2.1.2"; sha256 = "1507hnpr9my3z4w1r6xk5n0s1j3y6a2c2cnynj76za7cphxi1141"; }
		{ pname = "Microsoft.NETFramework.ReferenceAssemblies"; version = "1.0.0"; sha256 = "0na724xhvqm63vq9y18fl9jw9q2v99bdwr353378s5fsi11qzxp9"; }
		{ pname = "Microsoft.NETFramework.ReferenceAssemblies.net48"; version = "1.0.0"; sha256 = "1kl3nx89mkyn07rc3ny0mzrnyndzyk95bl5b0khkdkncqnha02zx"; }
		{ pname = "Microsoft.TestPlatform.ObjectModel"; version = "16.8.3"; sha256 = "0szyg2p18w9lhlp52iylrr97w3kdalab089imhc53x1850avddsg"; }
		{ pname = "Microsoft.TestPlatform.TestHost"; version = "16.8.3"; sha256 = "0lgjvzhh2gkcl1r1f9w97wv5zy0p8iga68ml6v10m9wxh640zdqr"; }
		{ pname = "MSTest.TestAdapter"; version = "2.1.2"; sha256 = "1390nyc0sf5c4j75cq58bzqjcw77sp2lmpllmm5sp8ysi0fjyfs5"; }
		{ pname = "MSTest.TestFramework"; version = "2.1.2"; sha256 = "1617q2accpa8fwy9n1snmjxyx2fz3phks62mdi45cl65kdin0x4z"; }
		{ pname = "Newtonsoft.Json"; version = "12.0.3"; sha256 = "17dzl305d835mzign8r15vkmav2hq8l6g7942dfjpnzr17wwl89x"; }
		{ pname = "OpenTK"; version = "3.3.0"; sha256 = "1nnwam2lc9zmadi0k2nvv764m6n8sn0nk8cdgg8574i3dn0llmha"; }
		{ pname = "runtime.native.System"; version = "4.0.0"; sha256 = "1ppk69xk59ggacj9n7g6fyxvzmk1g5p4fkijm0d7xqfkig98qrkf"; }
		{ pname = "SharpCompress"; version = "0.24.0"; sha256 = "13bzq8ggipr5254654l2ndm6jdxj9ggandy01gpjxnjwy4jhaz9p"; }
		{ pname = "System.Drawing.Common"; version = "5.0.0"; sha256 = "0fag8hr2v9bswrsjka311lhbr1a43yzcc36j4fadz0f0kl2hby7h"; }
		{ pname = "System.Runtime.CompilerServices.Unsafe"; version = "4.5.2"; sha256 = "1vz4275fjij8inf31np78hw50al8nqkngk04p3xv5n4fcmf1grgi"; }
		{ pname = "System.Runtime.InteropServices.RuntimeInformation"; version = "4.0.0"; sha256 = "0glmvarf3jz5xh22iy3w9v3wyragcm4hfdr17v90vs7vcrm7fgp6"; }
		{ pname = "System.Text.Encoding.CodePages"; version = "4.5.1"; sha256 = "1z21qyfs6sg76rp68qdx0c9iy57naan89pg7p6i3qpj8kyzn921w"; }
	];
	only-2_6_3 = only-2_6_2-through-2_9 ++ only-2_6_3_and-2_7 ++ only-2_6_3-through-2_8 ++ only-2_6_3-through-2_9;
	since-2_6_3 = [
		{ pname = "DotNetAnalyzers.DocumentationAnalyzers"; version = "1.0.0-beta.59"; sha256 = "03kllr1f52jq5a0b1fa2s57yz1bp0awh36n4dk679nqgbg6m8flj"; }
		{ pname = "DotNetAnalyzers.DocumentationAnalyzers.Unstable"; version = "1.0.0.59"; sha256 = "1491q3rby0ysm2bszr33r9h48wk4bz3bz748ziz8p2rklcqrhn5f"; }
		{ pname = "Newtonsoft.Json"; version = "13.0.1"; sha256 = "0fijg0w6iwap8gvzyjnndds0q4b8anwxxvik7y8vgq97dram4srb"; }
		{ pname = "System.Reflection.Emit"; version = "4.7.0"; sha256 = "121l1z2ypwg02yz84dy6gr82phpys0njk7yask3sihgy214w43qp"; }
	];

	until-2_7 = [];
	only-2_7 = only-2_6_2-through-2_9 ++ only-2_6_3_and-2_7 ++ only-2_6_3-through-2_8 ++ only-2_6_3-through-2_9;
	since-2_7 = [];

	until-2_8 = [];
	only-2_8 = only-2_6_2-through-2_9 ++ only-2_6_3-through-2_8 ++ only-2_6_3-through-2_9 ++ only-2_8-and-2_9;
	since-2_8 = [
		{ pname = "System.Runtime.CompilerServices.Unsafe"; version = "5.0.0"; sha256 = "02k25ivn50dmqx5jn8hawwmz24yf0454fjd823qk6lygj9513q4x"; }
		{ pname = "System.Text.Encoding.CodePages"; version = "5.0.0"; sha256 = "1bn2pzaaq4wx9ixirr8151vm5hynn3lmrljcgjx9yghmm4k677k0"; }
	];

	until-2_9 = [
		{ pname = "Nullable"; version = "1.3.0"; sha256 = "1h4m6as8ahkjm6adz42rnp33kgxgkn0aj7a1m09c73w4fps1ypcd"; }
		{ pname = "OpenTK.GLControl"; version = "3.1.0"; sha256 = "0j9h89qzwicpd396s49pp51vs3h92ksai444k68w85x62mg07cnm"; }
	];
	only-2_9 = only-2_6_2-through-2_9 ++ only-2_6_3-through-2_9 ++ only-2_8-and-2_9 ++ [
		{ pname = "Menees.Analyzers"; version = "3.0.8"; sha256 = "1apv06cmnrakaylyh85hjyn380cnj0h53j3pakyp0f4jnpgw0bgf"; }
		{ pname = "Meziantou.Analyzer"; version = "1.0.707"; sha256 = "09drs16fr0xly4k8drznw7pa5f2byjc9km0pm0c3rrhl7jsz4ds5"; }
		{ pname = "System.Drawing.Common"; version = "5.0.3"; sha256 = "1rnvqh5hwsqvxgn9g8g98sppg32llhk4y45lgv4dw0q6k945pgcy"; }
	];
	since-2_9 = [
		{ pname = "Google.FlatBuffers"; version = "22.9.24"; sha256 = "0v72xrzcd7pphjizi2y5amk11nqjvhm7qqcb899rypl91a8vxw04"; }
		{ pname = "Microsoft.Bcl.HashCode"; version = "1.1.1"; sha256 = "0xwfph92p92d8hgrdiaka4cazqsjpg4ywfxfx6qbk3939f29kzl0"; }
		{ pname = "Nullable"; version = "1.3.1"; sha256 = "0hwrr4q22c0i056dqy3v431rxjv7md910ihz0pjsi16qxsbpw7p7"; }
		{ pname = "OpenTK"; version = "3.3.3"; sha256 = "1ab67cmwd5nzdp1476m2ikb3m7a7hhn7wdfl6zfzp0wngzbcvzv9"; }
		{ pname = "OpenTK.GLControl"; version = "3.3.3"; sha256 = "0nc9yykylkf4qmrzbkn368iaq5jzly8ryw99sggd293qhp2s1kqm"; }
		{ pname = "StyleCop.Analyzers"; version = "1.2.0-beta.435"; sha256 = "0dirz0av24ds2k7hgpss15y4wlhwlzz22qdjvkq0n3g3sxcckrsy"; }
		{ pname = "StyleCop.Analyzers.Unstable"; version = "1.2.0.435"; sha256 = "1jv4ha4y2c9922n21yf2dvfkmi8qfa8z28gk5zsqdyck08izp9mh"; }
	];

	until-2_9_1 = [
		{ pname = "Microsoft.CSharp"; version = "4.0.1"; sha256 = "0zxc0apx1gcx361jlq8smc9pfdgmyjh6hpka8dypc9w23nlsh6yj"; }
		{ pname = "Microsoft.NETCore.Platforms"; version = "1.0.1"; sha256 = "01al6cfxp68dscl15z7rxfw9zvhm64dncsw09a1vmdkacsa2v6lr"; }
		{ pname = "Microsoft.NETCore.Platforms"; version = "5.0.0"; sha256 = "0mwpwdflidzgzfx2dlpkvvnkgkr2ayaf0s80737h4wa35gaj11rc"; }
		{ pname = "Microsoft.NETCore.Targets"; version = "1.0.1"; sha256 = "0ppdkwy6s9p7x9jix3v4402wb171cdiibq7js7i13nxpdky7074p"; }
		{ pname = "Microsoft.Win32.SystemEvents"; version = "5.0.0"; sha256 = "0sja4ba0mrvdamn0r9mhq38b9dxi08yb3c1hzh29n1z6ws1hlrcq"; }
		{ pname = "Newtonsoft.Json"; version = "9.0.1"; sha256 = "0mcy0i7pnfpqm4pcaiyzzji4g0c8i3a5gjz28rrr28110np8304r"; }
		{ pname = "NuGet.Frameworks"; version = "5.0.0"; sha256 = "18ijvmj13cwjdrrm52c8fpq021531zaz4mj4b4zapxaqzzxf2qjr"; }
		{ pname = "System.Collections"; version = "4.0.11"; sha256 = "1ga40f5lrwldiyw6vy67d0sg7jd7ww6kgwbksm19wrvq9hr0bsm6"; }
		{ pname = "System.Diagnostics.Debug"; version = "4.0.11"; sha256 = "0gmjghrqmlgzxivd2xl50ncbglb7ljzb66rlx8ws6dv8jm0d5siz"; }
		{ pname = "System.Diagnostics.Tools"; version = "4.0.1"; sha256 = "19cknvg07yhakcvpxg3cxa0bwadplin6kyxd8mpjjpwnp56nl85x"; }
		{ pname = "System.Dynamic.Runtime"; version = "4.0.11"; sha256 = "1pla2dx8gkidf7xkciig6nifdsb494axjvzvann8g2lp3dbqasm9"; }
		{ pname = "System.Globalization"; version = "4.0.11"; sha256 = "070c5jbas2v7smm660zaf1gh0489xanjqymkvafcs4f8cdrs1d5d"; }
		{ pname = "System.IO"; version = "4.1.0"; sha256 = "1g0yb8p11vfd0kbkyzlfsbsp5z44lwsvyc0h3dpw6vqnbi035ajp"; }
		{ pname = "System.IO.FileSystem"; version = "4.0.1"; sha256 = "0kgfpw6w4djqra3w5crrg8xivbanh1w9dh3qapb28q060wb9flp1"; }
		{ pname = "System.IO.FileSystem.Primitives"; version = "4.0.1"; sha256 = "1s0mniajj3lvbyf7vfb5shp4ink5yibsx945k6lvxa96r8la1612"; }
		{ pname = "System.Linq"; version = "4.1.0"; sha256 = "1ppg83svb39hj4hpp5k7kcryzrf3sfnm08vxd5sm2drrijsla2k5"; }
		{ pname = "System.Linq.Expressions"; version = "4.1.0"; sha256 = "1gpdxl6ip06cnab7n3zlcg6mqp7kknf73s8wjinzi4p0apw82fpg"; }
		{ pname = "System.ObjectModel"; version = "4.0.12"; sha256 = "1sybkfi60a4588xn34nd9a58png36i0xr4y4v4kqpg8wlvy5krrj"; }
		{ pname = "System.Reflection"; version = "4.1.0"; sha256 = "1js89429pfw79mxvbzp8p3q93il6rdff332hddhzi5wqglc4gml9"; }
		{ pname = "System.Reflection.Emit"; version = "4.0.1"; sha256 = "0ydqcsvh6smi41gyaakglnv252625hf29f7kywy2c70nhii2ylqp"; }
		{ pname = "System.Reflection.Emit.ILGeneration"; version = "4.0.1"; sha256 = "1pcd2ig6bg144y10w7yxgc9d22r7c7ww7qn1frdfwgxr24j9wvv0"; }
		{ pname = "System.Reflection.Emit.Lightweight"; version = "4.0.1"; sha256 = "1s4b043zdbx9k39lfhvsk68msv1nxbidhkq6nbm27q7sf8xcsnxr"; }
		{ pname = "System.Reflection.Extensions"; version = "4.0.1"; sha256 = "0m7wqwq0zqq9gbpiqvgk3sr92cbrw7cp3xn53xvw7zj6rz6fdirn"; }
		{ pname = "System.Reflection.Primitives"; version = "4.0.1"; sha256 = "1bangaabhsl4k9fg8khn83wm6yial8ik1sza7401621jc6jrym28"; }
		{ pname = "System.Reflection.TypeExtensions"; version = "4.1.0"; sha256 = "1bjli8a7sc7jlxqgcagl9nh8axzfl11f4ld3rjqsyxc516iijij7"; }
		{ pname = "System.Resources.ResourceManager"; version = "4.0.1"; sha256 = "0b4i7mncaf8cnai85jv3wnw6hps140cxz8vylv2bik6wyzgvz7bi"; }
		{ pname = "System.Runtime"; version = "4.1.0"; sha256 = "02hdkgk13rvsd6r9yafbwzss8kr55wnj8d5c7xjnp8gqrwc8sn0m"; }
		{ pname = "System.Runtime.Extensions"; version = "4.1.0"; sha256 = "0rw4rm4vsm3h3szxp9iijc3ksyviwsv6f63dng3vhqyg4vjdkc2z"; }
		{ pname = "System.Runtime.Handles"; version = "4.0.1"; sha256 = "1g0zrdi5508v49pfm3iii2hn6nm00bgvfpjq1zxknfjrxxa20r4g"; }
		{ pname = "System.Runtime.InteropServices"; version = "4.1.0"; sha256 = "01kxqppx3dr3b6b286xafqilv4s2n0gqvfgzfd4z943ga9i81is1"; }
		{ pname = "System.Runtime.Serialization.Primitives"; version = "4.1.1"; sha256 = "042rfjixknlr6r10vx2pgf56yming8lkjikamg3g4v29ikk78h7k"; }
		{ pname = "System.Text.Encoding"; version = "4.0.11"; sha256 = "1dyqv0hijg265dwxg6l7aiv74102d6xjiwplh2ar1ly6xfaa4iiw"; }
		{ pname = "System.Text.Encoding.Extensions"; version = "4.0.11"; sha256 = "08nsfrpiwsg9x5ml4xyl3zyvjfdi4mvbqf93kjdh11j4fwkznizs"; }
		{ pname = "System.Text.RegularExpressions"; version = "4.1.0"; sha256 = "1mw7vfkkyd04yn2fbhm38msk7dz2xwvib14ygjsb8dq2lcvr18y7"; }
		{ pname = "System.Threading"; version = "4.0.11"; sha256 = "19x946h926bzvbsgj28csn46gak2crv2skpwsx80hbgazmkgb1ls"; }
		{ pname = "System.Threading.Tasks"; version = "4.0.11"; sha256 = "0nr1r41rak82qfa5m0lhk9mp0k93bvfd7bbd9sdzwx9mb36g28p5"; }
		{ pname = "System.Threading.Tasks.Extensions"; version = "4.0.0"; sha256 = "1cb51z062mvc2i8blpzmpn9d9mm4y307xrwi65di8ri18cz5r1zr"; }
		{ pname = "System.Xml.ReaderWriter"; version = "4.0.11"; sha256 = "0c6ky1jk5ada9m94wcadih98l6k1fvf6vi7vhn1msjixaha419l5"; }
		{ pname = "System.Xml.XDocument"; version = "4.0.11"; sha256 = "0n4lvpqzy9kc7qy1a4acwwd7b7pnvygv895az5640idl2y9zbz18"; }
	];
	only-2_9_1 = [
		{ pname = "Cyotek.Drawing.BitmapFont"; version = "2.0.4"; sha256 = "04n0lq9sqfjzyvkqav6qrc8v9fb7jv1n7jk0j4r6ivfbmffzq8if"; }
		{ pname = "JunitXml.TestLogger"; version = "3.0.124"; sha256 = "1s70k74bkw0fhfkylak289c2s7l6vijz7n5j64qyw7bjzncqs4n7"; }
		{ pname = "Menees.Analyzers"; version = "3.0.10"; sha256 = "1fgkr4x8cnjmn6xj106g1y5smc49x9qkmb8c42nx7sqg3lzwqpgb"; }
		{ pname = "Meziantou.Analyzer"; version = "2.0.33"; sha256 = "0lyi217pijxh7jw5xg47ks5r32s77g0xlw9nf42vjbmczfmhk23d"; }
		{ pname = "Microsoft.CodeCoverage"; version = "17.5.0"; sha256 = "0briw00gb5bz9k9kx00p6ghq47w501db7gb6ig5zzmz9hb8lw4a4"; }
		{ pname = "Microsoft.NET.Test.Sdk"; version = "17.5.0"; sha256 = "00gz2i8kx4mlq1ywj3imvf7wc6qzh0bsnynhw06z0mgyha1a21jy"; }
		{ pname = "Microsoft.NETFramework.ReferenceAssemblies"; version = "1.0.3"; sha256 = "0hc4d4d4358g5192mf8faijwk0bpf9pjwcfd3h85sr67j0zhj6hl"; }
		{ pname = "Microsoft.NETFramework.ReferenceAssemblies.net48"; version = "1.0.3"; sha256 = "18h4265rn5dy5d1igddsz1ilygcyyj4id4cn2qsr3sz7722k8zla"; }
		{ pname = "Microsoft.TestPlatform.ObjectModel"; version = "17.5.0"; sha256 = "0qkjyf3ky6xpjg5is2sdsawm99ka7fzgid2bvpglwmmawqgm8gls"; }
		{ pname = "Microsoft.TestPlatform.TestHost"; version = "17.5.0"; sha256 = "17g0k3r5n8grba8kg4nghjyhnq9w8v0w6c2nkyyygvfh8k8x9wh3"; }
		{ pname = "Microsoft.Win32.SystemEvents"; version = "6.0.0"; sha256 = "0c6pcj088g1yd1vs529q3ybgsd2vjlk5y1ic6dkmbhvrp5jibl9p"; }
		{ pname = "MSTest.TestAdapter"; version = "2.2.9"; sha256 = "0wvnpnwhfwbbgzdpqjwdmndda4x2ha8zq3pbd8x008g2kj1d5ggl"; }
		{ pname = "MSTest.TestFramework"; version = "2.2.9"; sha256 = "1wd57ky7nxmddp1nn3sy6p9w1jdfg89z1yw0yrg16byn501jrs23"; }
		{ pname = "Newtonsoft.Json"; version = "13.0.3"; sha256 = "0xrwysmrn4midrjal8g2hr1bbg38iyisl0svamb11arqws4w2bw7"; }
		{ pname = "NuGet.Frameworks"; version = "5.11.0"; sha256 = "0wv26gq39hfqw9md32amr5771s73f5zn1z9vs4y77cgynxr73s4z"; }
		{ pname = "SharpCompress"; version = "0.31.0"; sha256 = "01az7amjkxjbya5rdcqwxzrh2d3kybf1gsd3617rsxvvxadyra1r"; }
		{ pname = "System.Collections.Immutable"; version = "7.0.0"; sha256 = "1n9122cy6v3qhsisc9lzwa1m1j62b8pi2678nsmnlyvfpk0zdagm"; }
		{ pname = "System.Drawing.Common"; version = "6.0.0"; sha256 = "02n8rzm58dac2np8b3xw8ychbvylja4nh6938l5k2fhyn40imlgz"; }
		{ pname = "System.Memory"; version = "4.5.5"; sha256 = "08jsfwimcarfzrhlyvjjid61j02irx6xsklf32rv57x2aaikvx0h"; }
		{ pname = "System.Resources.Extensions"; version = "7.0.0"; sha256 = "0d5gk5g5qqkwa728jwx9yabgjvgywsy6k8r5vgqv2dmlvjrqflb4"; }
		{ pname = "System.Runtime.CompilerServices.Unsafe"; version = "6.0.0"; sha256 = "0qm741kh4rh57wky16sq4m0v05fxmkjjr87krycf5vp9f0zbahbc"; }
	];
	since-2_9_1 = [];
}
