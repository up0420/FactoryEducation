using System;
using System.Collections.Generic;
using UnityEngine;

public enum NodeRole { Host, Client }

    [Header("Role & IDs")]
    public NodeRole role = NodeRole.Host; // 한 대만 Host, 나머지 Client
    public string myId = "hmd-01";
    public string sessionCode = "000000";

    [Header("Refs")]
    public OscTransport transport;

    int _seq;
    float _hbTimer;
    const float HeartbeatInterval = 2f;
    readonly Dictionary<string, long> _lastSeen = new(); // id -> ts

        if (transport == null) transport = GetComponent<OscTransport>();
        transport.OnRawMessage += HandleRaw;
        if (role == NodeRole.Client) SendJoin();
    }

        _hbTimer += Time.deltaTime;
            _hbTimer = 0;
            SendPing();
        }
    }

        if (msg == null || string.IsNullOrEmpty(msg.path)) return;

            case "/session/join":
                    // 간단 검증(코드 일치만)
                }
                break;

            case "/session/ready":
                    Debug.Log($"Client ready: {msg.id}");
                    if (AllReady()) BroadcastStart();
                }
                break;

            case "/session/start":
                GameSignals.SessionStart?.Invoke();
                break;

            case "/net/ping":
                break;

            case "/ppe/select":
                    SendPpeConfirm(msg.id, correct, bonus);
                }
                break;

            case "/emergency/stop":
                break;

            case "/pose":
                // 포즈 갱신(렌더러/아바타에 반영)
                break;
        }
    }

        long now = OscMessage.NowMs();
    }

    }

        var m = NewMsg("/session/start");
        Broadcast(m);
    }

        var m = NewMsg("/session/join");
        m.reason = sessionCode; // reason 필드에 세션코드 전송(간단화)
        Send(m);
    }

        var m = NewMsg("/session/ready");
        Broadcast(m);
    }

        var m = NewMsg("/net/ping");
        Send(m);
    }

        var m = NewMsg("/net/pong");
        Send(m);
    }

        var m = NewMsg("/pose");
        m.px = p.x; m.py = p.y; m.pz = p.z;
        m.qx = q.x; m.qy = q.y; m.qz = q.z; m.qw = q.w;
        Send(m);
    }

        var m = NewMsg("/ppe/select");
        m.picks = picks;
        Send(m);
    }

        var m = NewMsg("/ppe/confirm");
        m.id = targetId;
        m.correctCount = correct;
        m.bonus = bonus;
        Broadcast(m);
    }

    // Helpers
            path = path,
            id = myId,
            ts = OscMessage.NowMs(),
            seq = ++_seq
        };
    }

    void Send(OscMessage m) => transport.SendJson(OscMessage.ToJson(m));
    void Broadcast(OscMessage m) => transport.SendJson(OscMessage.ToJson(m));
    void Broadcast(string json) => transport.SendJson(json);
}
