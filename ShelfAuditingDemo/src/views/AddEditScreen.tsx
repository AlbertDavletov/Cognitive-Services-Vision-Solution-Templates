import React from 'react'
import { 
    View, 
    Image, 
    Text, 
    TouchableOpacity, 
    StyleSheet,
} from 'react-native'
import { NavigationScreenProp, NavigationState, NavigationParams } from 'react-navigation'
import { ImageWithRegions } from '../components/uikit'

interface AddEditScreenProps {
    navigation: NavigationScreenProp<NavigationState, NavigationParams>;
}

interface AddEditScreenState {
    data: Array<any>,
    imageSource: any,
    tags: Array<any>;
    selectedTag: any;
    selectedRegions: Array<any>;
}

export class AddEditScreen extends React.Component<AddEditScreenProps, AddEditScreenState> {
    static navigationOptions = ( { navigation } : { navigation : NavigationScreenProp<NavigationState,NavigationParams> }) => {
        const { params } = navigation.state;
        return { 
            title: 'Add/Edit item',
            headerRight: () => (
                <TouchableOpacity 
                    activeOpacity={0.6} 
                    onPress={() => { 
                        if (params?.applyChanges) {
                            params.applyChanges(navigation);
                        }
                    }}
                    style={{ padding: 4, marginRight: 10 }}>
                    <Text style={{color: 'white', fontSize: 16, fontWeight: 'bold' }}>Apply</Text>
                </TouchableOpacity>
            )
        }
    }

    constructor(props: AddEditScreenProps) {
        super(props);
        const { navigation } = props;
        const tags = navigation.getParam('tags', []);
        const tagCollection = this.getTagCollection(tags);

        this.state = {
            data: [],
            imageSource: null,
            tags: tagCollection,
            selectedTag: null,
            selectedRegions: []
        }
    }

    componentDidMount() {
        const { navigation } = this.props;
        navigation.setParams({ applyChanges: this.applyChanges });

        let data = navigation.getParam('data', []);
        let selectedRegions = navigation.getParam('selectedRegions', []);
        let imageSource = navigation.getParam('imageSource', {});

        this.setState({
            data: data,
            imageSource: imageSource,
            selectedRegions: selectedRegions
        });

        let selectedTag;
        if (selectedRegions.length > 0) {
            let model = selectedRegions[0].model;
            selectedTag = {
                id: model.tagId,
                name: model.tagName,
                imageUrl: 'https://intelligentkioskstore.blob.core.windows.net/shelf-auditing/Mars/Products/' + model.tagName.toLocaleLowerCase() + '.jpg'
            };

            this.setState({
                selectedTag: selectedTag
            });
        }
    }

    render() {
        const { 
            mainContainer, 
            labelContainer, 
            imageThumbnail, 
            button, 
            buttonLabel,
            tagContainer, 
            tagLabel, 
            line 
        } = this.styles;

        let imageWithRegionsComponent;
        if (this.state.imageSource && this.state.data) {
            imageWithRegionsComponent = (
                <ImageWithRegions ref='editableImageWithRegions'
                    mode='edit'
                    imageSource={this.state.imageSource}
                    regions={this.state.data}
                    editableRegions={this.state.selectedRegions}
                />
            );
        }

        let selectedTagComponent;
        if (this.state.selectedTag) {
            let tag = this.state.selectedTag;
            selectedTagComponent = 
                <View style={{ flexDirection: 'row'}}>
                    <View style={{ padding: 10 }}>
                        { tag.name.toLocaleLowerCase() != 'product' && tag.name.toLocaleLowerCase() != 'gap' &&
                            <Image style={imageThumbnail} source={{ uri: tag.imageUrl }} />
                        }
                        { tag.name.toLocaleLowerCase() == 'product' &&
                            <Image style={imageThumbnail} source={require('../assets/product.jpg')} />
                        }
                        { tag.name.toLocaleLowerCase() == 'gap' &&
                            <Image style={imageThumbnail} source={require('../assets/gap.jpg')} />
                        }
                    </View>
                    
                    <View style={{ flex: 1 }}>
                        <View style={tagContainer}>
                            <Text numberOfLines={1} style={tagLabel}>{tag.name}</Text>
                        </View>
                        
                        <View style={line}/>
                    </View>
                </View>
        }

        return (
            <View style={mainContainer}>

                { imageWithRegionsComponent }
                
                <View style={labelContainer}>
                    <TouchableOpacity activeOpacity={0.6} onPress={() => this.onChooseLabel()} style={button}>
                        <Text style={buttonLabel}>Choose new label</Text>
                    </TouchableOpacity>

                    { selectedTagComponent }
                </View>
            </View>
        );
    }

    getTagCollection(tags: Array<any>) {
        let tagCollection = Array<any>();
        tags.forEach(t => {
            tagCollection.push({
                id: t.id,
                name: t.name,
                imageUrl: 'https://intelligentkioskstore.blob.core.windows.net/shelf-auditing/Mars/Products/' + t.name.toLocaleLowerCase() + '.jpg'
            });
        });

        return tagCollection;
    }

    onChooseLabel() {
        const { navigate } = this.props.navigation;
        navigate('TagCollection', { tags: this.state.tags, returnData: this.returnData.bind(this) });
    }

    returnData(selectedTag: any) {
        this.state.selectedRegions.forEach(r => {
            r.displayName = selectedTag.name,
            r.model.tagId = selectedTag.id,
            r.model.tagName = selectedTag.name
        });
        this.setState({ selectedTag: selectedTag });
    }

    applyChanges(navigation: NavigationScreenProp<NavigationState, NavigationParams>) {
        const { params } = navigation.state;
        if (params?.addEditModeCallback) {
            params.addEditModeCallback(params.selectedRegions);
        }
        navigation.goBack();
    }

    styles = StyleSheet.create({
        mainContainer: {
            flex: 1,
            backgroundColor: 'black',
            padding: 0
        },
        labelContainer: {
            height: '25%',
            padding: 16
        },
        imageThumbnail: {
            justifyContent: 'center',
            alignItems: 'center',
            height: 40,
            width: 40
        },
        button: {
            padding: 6, 
            marginBottom: 10
        },
        buttonLabel: {
            color: '#0078D4', 
            fontSize: 16, 
            fontWeight: 'bold', 
            textAlign: 'center' 
        },
        tagContainer: {
            flex: 1, 
            justifyContent: 'center'
        },
        tagLabel: {
            color: 'white', 
            textAlign: 'left', 
            padding: 5
        },
        line: {
            height: 1, 
            backgroundColor: 'white', 
            opacity: 0.2
        }
    })
}
