import React from 'react'
import { View, Text, StyleSheet } from 'react-native'
import { RegionState } from '../../models'
import { Util } from '../../../Util'
import { styles } from './ObjectRegion.style'

interface RegionProps {
    position: any;
    data: any;
}

export const ObjectRegion = (params: RegionProps) => {
    const { position, data } = params;
    const positionStyle = StyleSheet.create({ 
        left: position.left, 
        top: position.top,
        width: position.width,
        height: position.height
    });

    let component;
    switch (data.state) {
        case RegionState.Selected:
            component = <View style={[positionStyle, styles.selectedRegion, { borderColor: data.color }]}>
                            <View style={[styles.selectedRegionLabelPanel, { borderColor: data.color, backgroundColor: data.color }]}>
                                <Text numberOfLines={1} style={styles.selectedRegionLabel}>{data.title}</Text>  
                            </View>
                        </View>;
            break;

        case RegionState.Disabled:
            component = <View style={[positionStyle, styles.disabledRegion, { 
                                      borderColor: data.color,
                                      backgroundColor: Util.SetOpacityToColor(data.color, 0.4) 
                            }]}>
                            <View style={[styles.smallCircle, { backgroundColor: data.color }]}/>
                        </View>;
            break;

        case RegionState.Active:
        default:
            component = <View 
                            style={[positionStyle, styles.activeRegion, { 
                                    borderColor: data.color, 
                                    backgroundColor: Util.SetOpacityToColor(data.color, 0.6) 
                            }]}>
                            <View style={styles.circle}/>
                        </View>;
            break;
    }
    return component;
}
